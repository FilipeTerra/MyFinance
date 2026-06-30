"""
api_tools.py — Ferramentas de API autenticadas para o Agente Financeiro

Arquitetura HTTP:
  Todas as ferramentas são async, usando httpx.AsyncClient em vez de requests.
  Isso evita que chamadas HTTP bloqueiem o event loop durante graph.ainvoke().

  _buscar_todas_transacoes usa asyncio.gather para disparar as requisições de
  transações de todas as contas em paralelo — para um usuário com N contas,
  o tempo de resposta cai de N × latência para 1 × latência (maior conta).

  O AsyncClient é criado uma vez por invocação de make_api_tools e compartilhado
  por todas as closures da mesma sessão (JWT compartilhado, pool de conexões reutilizado).
"""

import asyncio
import os
from datetime import datetime, timedelta, timezone

import httpx
from dotenv import load_dotenv
from langchain_core.tools import tool
from pydantic import BaseModel, Field

load_dotenv()

_API_BASE_URL = os.getenv("API_URL", "http://localhost:5088/api")

_ERR_OFFLINE = "Erro: A API financeira está offline ou inacessível."
_ERR_SESSAO  = "Sessão expirada. O usuário precisa fazer login novamente."

class SimularEstresseOrcamentoInput(BaseModel):
    descricao_nova_despesa: str = Field(
        ...,
        description="O que o usuário deseja assumir. Ex: 'Financiamento do carro', 'Aluguel mais caro'."
    )
    valor_mensal: float = Field(
        ...,
        description="O valor mensal da nova despesa em reais. Ex: 1200.00"
    )
    tipo_despesa: str = Field(
        ...,
        description="Classifique a despesa em: 'essencial' (moradia, transporte, saúde), 'estilo_de_vida' (lazer, assinaturas) ou 'divida'."
    )

def make_api_tools(jwt_token: str) -> list:
    """
    Factory que cria as ferramentas HTTP com o JWT baked-in via closure.
    O LLM nunca vê o token — ele apenas chama as ferramentas pelo nome.

    Um único AsyncClient é criado por chamada a make_api_tools. Todas as tools
    da mesma requisição reutilizam esse cliente (headers e pool de conexão
    compartilhados). O cliente é finalizado pelo GC quando o grafo encerra.
    """
    _client = httpx.AsyncClient(
        headers={"Authorization": f"Bearer {jwt_token}"},
        timeout=10.0,
    )

    # =========================================================================
    # Helpers HTTP privados — thin wrappers sobre o AsyncClient
    # =========================================================================

    async def _get(path: str) -> httpx.Response:
        return await _client.get(f"{_API_BASE_URL}{path}")

    async def _post(path: str, payload: dict) -> httpx.Response:
        return await _client.post(f"{_API_BASE_URL}{path}", json=payload)

    # =========================================================================
    # Helper de período — pura computação, permanece síncrono
    # =========================================================================

    def _resolver_periodo(
        data_inicio: str,
        data_fim: str,
        ultimos_dias: int = 30,
    ) -> tuple:
        """
        Converte parâmetros do usuário em um intervalo de datas concreto.

        Prioridade:
          1. data_inicio (YYYY-MM-DD) fornecida → usa data_inicio + (data_fim ou agora).
          2. Nenhuma data → janela relativa de ultimos_dias a partir de agora.

        Returns:
            (dt_inicio, dt_fim, label, dias_periodo)
        """
        utc = timezone.utc
        agora = datetime.now(utc)

        if data_inicio.strip():
            try:
                dt_i = datetime.strptime(data_inicio.strip(), "%Y-%m-%d").replace(tzinfo=utc)
            except ValueError:
                dt_i = agora - timedelta(days=ultimos_dias)

            if data_fim.strip():
                try:
                    dt_f = datetime.strptime(data_fim.strip(), "%Y-%m-%d").replace(
                        hour=23, minute=59, second=59, tzinfo=utc
                    )
                except ValueError:
                    dt_f = agora
            else:
                dt_f = agora

            label = f"{dt_i.strftime('%d/%m/%Y')} a {dt_f.strftime('%d/%m/%Y')}"
        else:
            dt_f = agora
            dt_i = agora - timedelta(days=ultimos_dias)
            label = f"últimos {ultimos_dias} dias"

        dias_periodo = max(1, (dt_f - dt_i).days + 1)
        return dt_i, dt_f, label, dias_periodo

    # =========================================================================
    # Helper de busca concorrente — núcleo da otimização async
    # =========================================================================

    async def _buscar_transacoes_conta(
        account_id: str,
        dt_inicio: datetime,
        dt_fim: datetime,
    ) -> list:
        """
        Busca as transações de UMA conta no período e filtra pelo intervalo.
        Projetada para ser executada em paralelo via asyncio.gather — cada
        conta dispara sua própria requisição sem esperar as demais.
        Falhas individuais retornam lista vazia, não propagam exceção.
        """
        try:
            r = await _client.get(
                f"{_API_BASE_URL}/transactions/account/{account_id}"
            )
            if r.status_code != 200:
                return []
            resultado = []
            for t in (r.json() or []):
                date_str = t.get("date", "")
                try:
                    tx_date = datetime.fromisoformat(date_str.replace("Z", "+00:00"))
                    if tx_date.tzinfo is None:
                        tx_date = tx_date.replace(tzinfo=timezone.utc)
                    if dt_inicio <= tx_date <= dt_fim:
                        resultado.append(t)
                except Exception:
                    pass
            return resultado
        except Exception:
            return []

    async def _buscar_todas_transacoes(
        dt_inicio: datetime,
        dt_fim: datetime,
    ) -> list | str:
        """
        Busca transações de TODAS as contas do usuário em paralelo.

        Fluxo:
          1. Busca lista de contas (1 requisição sequencial obrigatória).
          2. Dispara 1 requisição por conta simultaneamente via asyncio.gather.
          3. Agrega os resultados, ignorando contas que falharam.

        Ganho: para N contas, o tempo de resposta é 1 × latência (maior conta)
        em vez de N × latência (sequencial com requests síncrono).
        """
        try:
            r = await _get("/accounts")
        except httpx.RequestError:
            return _ERR_OFFLINE

        if r.status_code == 401:
            return _ERR_SESSAO
        if r.status_code != 200:
            return "Não foi possível recuperar as contas do usuário."

        contas = r.json() or []
        if not contas:
            return "O usuário não possui contas cadastradas."

        ids = [c["id"] for c in contas if c.get("id")]
        if not ids:
            return "Nenhuma conta com ID válido encontrada."

        # Todas as requisições de transações em paralelo
        grupos = await asyncio.gather(
            *[_buscar_transacoes_conta(aid, dt_inicio, dt_fim) for aid in ids],
            return_exceptions=True,
        )

        todas = []
        for grupo in grupos:
            if isinstance(grupo, list):
                todas.extend(grupo)
        return todas

    # =========================================================================
    # Ferramentas de consulta simples
    # =========================================================================

    @tool
    async def consultar_saldos_contas() -> str:
        """Use esta ferramenta para verificar o saldo atual, listar as contas bancárias
        do usuário ou ver quanto dinheiro ele tem disponível. Retorna uma lista de contas e saldos."""
        try:
            r = await _get("/accounts")
            if r.status_code == 200:
                contas = r.json()
                return contas if contas else "O usuário ainda não possui contas cadastradas."
            if r.status_code == 401:
                return _ERR_SESSAO
            return f"Erro ao consultar contas (status {r.status_code})."
        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao consultar contas: {e}"

    @tool
    async def consultar_metas_financeiras() -> str:
        """Use esta ferramenta para verificar as metas financeiras do usuário
        (ex: comprar carro, fundo de emergência), ver o progresso, valores alvo e se a meta foi concluída."""
        try:
            r = await _get("/financial-goals")
            if r.status_code == 200:
                metas = r.json()
                return metas if metas else "O usuário ainda não possui metas financeiras cadastradas."
            if r.status_code == 401:
                return _ERR_SESSAO
            return f"Erro ao consultar metas (status {r.status_code})."
        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao consultar metas: {e}"

    # =========================================================================
    # Ferramentas de análise (usam _buscar_todas_transacoes em paralelo)
    # =========================================================================

    @tool
    async def consultar_transacoes_recentes(
        limite: int = 15,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta APENAS para listar transações individuais quando o usuário quiser
        ver o extrato ou histórico de movimentações específicas (ex: 'mostre minhas últimas transações',
        'o que comprei em maio', 'qual foi minha última compra'). NÃO use para análise de gastos
        por categoria ou resumo — use analisar_gastos_por_categoria e calcular_resumo_financeiro.
        Parâmetros: limite (padrão 15); data_inicio e data_fim em YYYY-MM-DD para períodos específicos
        (ex: maio de 2026 → data_inicio='2026-05-01', data_fim='2026-05-31')."""
        _TIPO_EMOJI = {1: "💰", 2: "💸", 3: "📈"}

        def _parse_date(t: dict) -> datetime:
            try:
                return datetime.fromisoformat(t.get("date", "").replace("Z", "+00:00"))
            except Exception:
                return datetime.min.replace(tzinfo=timezone.utc)

        try:
            dt_i, dt_f, label, _ = _resolver_periodo(data_inicio, data_fim, 90)
            transacoes = await _buscar_todas_transacoes(dt_i, dt_f)
            if isinstance(transacoes, str):
                return transacoes
            if not transacoes:
                return f"Nenhuma transação encontrada no período {label}."

            transacoes.sort(key=_parse_date, reverse=True)
            total = len(transacoes)
            exibidas = transacoes[:limite]

            linhas = [f"📋 {len(exibidas)} transações — {label}\n"]
            for t in exibidas:
                tx_type = t.get("type", 0)
                amount = t.get("amount", 0.0)
                emoji = _TIPO_EMOJI.get(tx_type, "•")
                sinal = "+" if amount > 0 else ""
                try:
                    data_fmt = _parse_date(t).strftime("%d/%m")
                except Exception:
                    data_fmt = "??"
                descricao = (t.get("description") or "Sem descrição")[:40]
                categoria = t.get("categoryName") or t.get("category") or "Sem categoria"
                linhas.append(
                    f"  {emoji} {data_fmt} | {descricao} | {categoria} | {sinal}R$ {abs(amount):,.2f}"
                )

            if total > limite:
                linhas.append(f"\n  ℹ️ Exibindo {limite} de {total} transações no período.")

            return "\n".join(linhas)

        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao consultar transações: {e}"

    @tool
    async def analisar_gastos_por_categoria(
        ultimos_dias: int = 30,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta para analisar onde o usuário está gastando mais dinheiro,
        identificar padrões de consumo, responder 'onde gasto mais?', 'como melhorar meus gastos?'
        ou qualquer pergunta sobre categorias de despesas. Agrupa despesas por categoria e mostra
        totais e percentuais. Use data_inicio e data_fim (YYYY-MM-DD) para períodos específicos
        (ex: maio de 2026 → data_inicio='2026-05-01', data_fim='2026-05-31'); omita para usar
        ultimos_dias (padrão 30)."""
        try:
            dt_i, dt_f, label, _ = _resolver_periodo(data_inicio, data_fim, ultimos_dias)
            transacoes = await _buscar_todas_transacoes(dt_i, dt_f)
            if isinstance(transacoes, str):
                return transacoes

            gastos: dict[str, float] = {}
            total_despesas = 0.0

            for t in transacoes:
                tx_type = t.get("type", 0)
                amount = t.get("amount", 0)
                if amount >= 0 or tx_type == 3:
                    continue
                categoria = (
                    t.get("categoryName")
                    or t.get("category")
                    or "Sem categoria"
                )
                valor = abs(amount)
                gastos[categoria] = gastos.get(categoria, 0.0) + valor
                total_despesas += valor

            if not gastos:
                return f"Nenhuma despesa encontrada no período {label}."

            linhas = [f"📊 Gastos por categoria — {label}\n"]
            for cat, total in sorted(gastos.items(), key=lambda x: x[1], reverse=True):
                pct = (total / total_despesas * 100) if total_despesas > 0 else 0
                linhas.append(f"  • {cat}: R$ {total:,.2f} ({pct:.1f}%)")
            linhas.append(f"\n  💸 Total gasto: R$ {total_despesas:,.2f}")
            return "\n".join(linhas)

        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao analisar gastos: {e}"

    @tool
    async def relatorio_mensal_por_categoria(
        filtro_categoria: str,
        ultimos_meses: int = 3,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta quando o usuário quiser um relatório detalhado de gastos em uma
        categoria ou tipo de gasto específico (ex: 'transporte', 'uber', 'alimentação', 'lazer')
        quebrado mês a mês. Exemplos: 'quanto gastei com uber nos últimos 3 meses?',
        'relatório de alimentação em maio de 2026', 'quanto gastei com transporte por mês?'.
        Parâmetros: filtro_categoria (palavra-chave, ex: 'uber', 'alimentação'), ultimos_meses
        (padrão 3); use data_inicio e data_fim (YYYY-MM-DD) para períodos específicos."""
        try:
            dt_i, dt_f, label, _ = _resolver_periodo(data_inicio, data_fim, ultimos_meses * 31)
            if not data_inicio.strip():
                label = f"últimos {ultimos_meses} meses"
            transacoes = await _buscar_todas_transacoes(dt_i, dt_f)
            if isinstance(transacoes, str):
                return transacoes

            filtro = filtro_categoria.lower().strip()
            meses: dict[str, dict] = {}

            for t in transacoes:
                tx_type = t.get("type", 0)
                amount = t.get("amount", 0)
                if amount >= 0 or tx_type == 3:
                    continue

                categoria = (t.get("categoryName") or t.get("category") or "").lower()
                descricao = (t.get("description") or "").lower()
                if filtro not in categoria and filtro not in descricao:
                    continue

                date_str = t.get("date", "")
                try:
                    tx_date = datetime.fromisoformat(date_str.replace("Z", "+00:00"))
                    chave_mes = tx_date.strftime("%Y-%m")
                    label_mes = tx_date.strftime("%B/%Y").capitalize()
                except Exception:
                    chave_mes = "desconhecido"
                    label_mes = "Data desconhecida"

                if chave_mes not in meses:
                    meses[chave_mes] = {"label": label_mes, "total": 0.0, "transacoes": []}
                valor = abs(amount)
                meses[chave_mes]["total"] += valor
                meses[chave_mes]["transacoes"].append({
                    "data": tx_date.strftime("%d/%m") if date_str else "??",
                    "descricao": t.get("description", "Sem descrição"),
                    "categoria": t.get("categoryName") or t.get("category") or "Sem categoria",
                    "valor": valor,
                })

            if not meses:
                return f"Nenhuma despesa encontrada com o filtro '{filtro_categoria}' no período {label}."

            _MAX_TX_POR_MES = 5

            total_geral = sum(m["total"] for m in meses.values())
            linhas = [f"📊 Relatório de '{filtro_categoria}' — {label}\n"]

            for chave in sorted(meses.keys(), reverse=True):
                mes = meses[chave]
                linhas.append(f"\n📅 {mes['label']} — R$ {mes['total']:,.2f}")
                ordenadas = sorted(mes["transacoes"], key=lambda x: x["valor"], reverse=True)
                for tx in ordenadas[:_MAX_TX_POR_MES]:
                    linhas.append(
                        f"  • {tx['data']} | {tx['descricao'][:40]} | {tx['categoria']} | R$ {tx['valor']:,.2f}"
                    )
                resto = ordenadas[_MAX_TX_POR_MES:]
                if resto:
                    valor_resto = sum(r["valor"] for r in resto)
                    linhas.append(f"  ... e mais {len(resto)} transação(ões) — R$ {valor_resto:,.2f}")

            linhas.append(f"\n💸 Total no período: R$ {total_geral:,.2f}")
            linhas.append(f"📈 Média mensal: R$ {total_geral / len(meses):,.2f}")
            return "\n".join(linhas)

        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao gerar relatório: {e}"

    @tool
    async def calcular_resumo_financeiro(
        ultimos_dias: int = 30,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta para obter um raio-x completo das finanças do usuário: receitas,
        despesas, investimentos, saldo líquido, gasto médio diário, categoria com maior gasto,
        maior despesa única e taxa de poupança. Use quando o usuário perguntar sobre saúde financeira,
        balanço do mês, situação geral, quanto está poupando ou investindo.
        Use data_inicio e data_fim (YYYY-MM-DD) para períodos específicos
        (ex: maio de 2026 → data_inicio='2026-05-01', data_fim='2026-05-31'); omita para usar
        ultimos_dias (padrão 30)."""
        try:
            dt_i, dt_f, label, dias_periodo = _resolver_periodo(data_inicio, data_fim, ultimos_dias)
            transacoes = await _buscar_todas_transacoes(dt_i, dt_f)
            if isinstance(transacoes, str):
                return transacoes

            if not transacoes:
                return f"Nenhuma transação encontrada no período {label}."

            total_receitas = 0.0
            total_despesas = 0.0
            total_investimentos = 0.0
            gastos_categoria: dict[str, float] = {}
            maior_despesa_valor = 0.0
            maior_despesa_desc = "N/A"

            for t in transacoes:
                tx_type = t.get("type", 0)
                amount = t.get("amount", 0)
                categoria = t.get("categoryName") or t.get("category") or "Sem categoria"
                descricao = (t.get("description") or "Sem descrição")[:50]

                if tx_type == 3:
                    total_investimentos += abs(amount)
                elif tx_type == 2 or (tx_type not in (1, 3) and amount < 0):
                    valor = abs(amount)
                    total_despesas += valor
                    gastos_categoria[categoria] = gastos_categoria.get(categoria, 0.0) + valor
                    if valor > maior_despesa_valor:
                        maior_despesa_valor = valor
                        maior_despesa_desc = descricao
                elif tx_type == 1 or (tx_type not in (2, 3) and amount > 0):
                    total_receitas += amount

            saldo_liquido = total_receitas - total_despesas - total_investimentos
            gasto_medio_diario = total_despesas / dias_periodo
            taxa_poupanca = (total_investimentos / total_receitas * 100) if total_receitas > 0 else 0.0
            situacao = "✅ positivo" if saldo_liquido >= 0 else "❌ negativo"

            if gastos_categoria:
                cat_vila = max(gastos_categoria, key=lambda k: gastos_categoria[k])
                cat_vila_valor = gastos_categoria[cat_vila]
            else:
                cat_vila, cat_vila_valor = "N/A", 0.0

            linhas = [
                f"## 📋 Raio-X Financeiro — {label}\n",
                "### 💰 Fluxo de Caixa",
                f"- **Receitas:** R$ {total_receitas:,.2f}",
                f"- **Despesas:** R$ {total_despesas:,.2f}",
                f"- **Investimentos/Aportes:** R$ {total_investimentos:,.2f}",
                f"- **Saldo líquido:** R$ {saldo_liquido:,.2f} ({situacao})\n",
                "### 📊 Indicadores",
                f"- **Gasto médio diário:** R$ {gasto_medio_diario:,.2f}",
                f"- **Taxa de poupança (investido/receita):** {taxa_poupanca:.1f}%\n",
                "### 🚨 Destaques",
                f"- **Categoria vilã:** {cat_vila} — R$ {cat_vila_valor:,.2f}",
                f"- **Maior despesa única:** {maior_despesa_desc} — R$ {maior_despesa_valor:,.2f}",
            ]

            return "\n".join(linhas)

        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao calcular resumo: {e}"

    # =========================================================================
    # Ferramentas de mutação (POST)
    # =========================================================================

    @tool
    async def criar_meta_financeira(nome: str, valor_alvo: float, data_limite: str) -> str:
        """Use esta ferramenta para criar uma nova meta financeira para o usuário APENAS quando
        ele pedir explicitamente. Exemplos: 'Quero criar uma meta para comprar um carro de 50000
        até dezembro', 'Cria uma meta de viagem de R$5000 para junho de 2026'.
        Recebe o nome da meta, o valor alvo e a data limite no formato 'YYYY-MM-DD'."""
        payload = {
            "name": nome,
            "targetAmount": valor_alvo,
            "deadline": f"{data_limite}T00:00:00",
        }
        try:
            r = await _post("/financial-goals", payload)
            if r.status_code in (200, 201):
                return (
                    f"✅ Meta criada com sucesso!\n"
                    f"  • Nome: {nome}\n"
                    f"  • Valor alvo: R$ {valor_alvo:,.2f}\n"
                    f"  • Prazo: {data_limite}"
                )
            if r.status_code == 401:
                return _ERR_SESSAO
            if r.status_code == 400:
                return f"Dados inválidos para criar a meta: {r.text}"
            return f"Erro ao criar meta (status {r.status_code})."
        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao criar meta: {e}"

    @tool
    async def realizar_aporte_meta(valor: float, goal_id: str, account_id: str) -> str:
        """Use esta ferramenta para investir ou guardar dinheiro em uma meta financeira específica.
        Recebe o valor, o ID da meta e o ID da conta de origem. Retorna sucesso ou erro."""
        try:
            r_cat = await _get("/categories")
            if r_cat.status_code != 200 or not r_cat.json():
                return "Erro: Nenhuma categoria encontrada. Crie pelo menos uma categoria antes de realizar um aporte."

            category_id = r_cat.json()[0]["id"]
            payload = {
                "amount": valor,
                "type": 3,
                "accountId": account_id,
                "financialGoalId": goal_id,
                "description": "Aporte na meta",
                "date": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S"),
                "categoryId": category_id,
            }
            r = await _post("/transactions", payload)
            if r.status_code in (200, 201):
                return f"✅ Aporte de R$ {valor:,.2f} realizado com sucesso na meta!"
            if r.status_code == 401:
                return _ERR_SESSAO
            if r.status_code == 400:
                return f"Dados inválidos: {r.text}"
            return f"Erro ao realizar aporte (status {r.status_code})."
        except httpx.RequestError:
            return _ERR_OFFLINE
        except Exception as e:
            return f"Erro inesperado ao realizar aporte: {e}"
        
    @tool(args_schema=SimularEstresseOrcamentoInput)
    async def simular_impacto_nova_despesa(descricao_nova_despesa: str, valor_mensal: float, tipo_despesa: str) -> dict:
        """
        Use esta ferramenta quando o usuário perguntar se 'dá conta' de assumir uma nova conta, 
        se o orçamento aguenta uma nova parcela, ou simular o impacto de um novo gasto fixo.
        """
        try:
            dt_fim = datetime.now(timezone.utc)
            dt_inicio = dt_fim - timedelta(days=30)
            
            transacoes = await _buscar_todas_transacoes(dt_inicio=dt_inicio, dt_fim=dt_fim)
            
            if isinstance(transacoes, str):
                return {"erro": transacoes}

            total_receitas = sum(t.get("amount", 0) for t in transacoes if t.get("type") == 1 or t.get("amount", 0) > 0)
            despesas_atuais = sum(abs(t.get("amount", 0)) for t in transacoes if t.get("type") == 2 or t.get("amount", 0) < 0)
            
            # Matemática determinística do cenário
            novo_total_despesas = despesas_atuais + valor_mensal
            saldo_liquido_atual = total_receitas - despesas_atuais
            novo_saldo_liquido = total_receitas - novo_total_despesas
            
            comprometimento_atual_pct = (despesas_atuais / total_receitas * 100) if total_receitas > 0 else 0
            novo_comprometimento_pct = (novo_total_despesas / total_receitas * 100) if total_receitas > 0 else 0

            # Classificação de Risco
            status_risco = "SEGURO"
            if novo_saldo_liquido < 0:
                status_risco = "CRÍTICO - Orçamento ficará negativo"
            elif novo_comprometimento_pct > 80:
                status_risco = "ALTO RISCO - Restará pouca margem de segurança"

            return {
                "analise": f"Impacto de assumir: {descricao_nova_despesa}",
                "cenario_atual": {
                    "receita_mensal": round(total_receitas, 2),
                    "despesa_mensal": round(despesas_atuais, 2),
                    "saldo_livre": round(saldo_liquido_atual, 2),
                    "comprometimento_renda_pct": round(comprometimento_atual_pct, 1)
                },
                "cenario_simulado": {
                    "nova_despesa": round(valor_mensal, 2),
                    "novo_total_despesas": round(novo_total_despesas, 2),
                    "novo_saldo_livre": round(novo_saldo_liquido, 2),
                    "novo_comprometimento_renda_pct": round(novo_comprometimento_pct, 1)
                },
                "status_de_risco": status_risco
            }
            
        except Exception as e:
            return {"erro": f"Falha ao simular cenário: {str(e)}"}

    return [
        consultar_saldos_contas,
        consultar_metas_financeiras,
        consultar_transacoes_recentes,
        criar_meta_financeira,
        realizar_aporte_meta,
        analisar_gastos_por_categoria,
        relatorio_mensal_por_categoria,
        calcular_resumo_financeiro,
        simular_impacto_nova_despesa
    ]
