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
import logging
from datetime import datetime, timedelta, timezone
import math

import httpx
from langchain_core.tools import tool
from pydantic import BaseModel, Field
from typing import Optional

from src.Infra.Config.settings import get_settings

_logger = logging.getLogger("myfinance.agent")

_API_BASE_URL = get_settings().api_url

_ERR_OFFLINE = "Erro: A API financeira está offline ou inacessível."
_ERR_SESSAO  = "Sessão expirada. O usuário precisa fazer login novamente."

# strftime('%B') devolve o mês no locale do sistema (geralmente inglês).
# Mapa fixo garante saída sempre em português, independente do ambiente.
_MESES_PT = {
    1: "Janeiro", 2: "Fevereiro", 3: "Março", 4: "Abril",
    5: "Maio", 6: "Junho", 7: "Julho", 8: "Agosto",
    9: "Setembro", 10: "Outubro", 11: "Novembro", 12: "Dezembro",
}

# Espelha Domain.Enums.InvestmentType do .NET — sem JsonStringEnumConverter
# configurado lá, o campo "tipo" chega aqui como inteiro, não como string.
_TIPO_INVESTIMENTO = {
    1: "Renda Fixa", 2: "Ação", 3: "FII", 4: "Cripto", 5: "ETF",
}
_TIPO_RENDA_FIXA = 1

# Palavras-chave usadas para identificar categorias/descrições de "estilo de
# vida" (gastos supérfluos) — mesma técnica de match usada em
# relatorio_mensal_por_categoria (substring case-insensitive em categoria + descrição).
_KEYWORDS_ESTILO_DE_VIDA = [
    "lazer", "restaurante", "assinatura", "role", "bar", "streaming", "delivery",
]

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
    
class SimularMetaIdealInput(BaseModel):
    objetivo_principal: str = Field(
        ...,
        description="O objetivo da meta. Ex: 'Reserva de Emergência', 'Entrada do Apartamento'."
    )
    valor_alvo_estimado: Optional[float] = Field(
        None,
        description="Opcional. O valor financeiro que o utilizador deseja atingir, se ele tiver mencionado. Ex: 50000 para um apartamento."
    )
    prazo_meses_desejado: Optional[int] = Field(
        None,
        description="Opcional. Em quantos meses o utilizador quer atingir a meta, se ele tiver mencionado."
    )
    
def _classificar_fluxo(transacoes: list) -> tuple[float, float, float]:
    """
    Classificador ÚNICO de fluxo de caixa — fonte da verdade para todas as
    tools de análise (resumo, impacto de despesa, meta ideal).

    Regra (espelha TransactionType do .NET):
      type 3 (Investment)          → aporte em meta/investimento (patrimônio,
                                     NÃO é despesa — o dinheiro continua do usuário)
      type 2 ou valor negativo     → despesa
      type 1 ou valor positivo     → receita

    Antes deste helper cada tool duplicava a classificação com regras
    ligeiramente diferentes (o simulador contava aporte como despesa; o resumo
    não), produzindo números conflitantes entre respostas do agente.

    Returns:
        (receitas, despesas, aportes) — todos como valores absolutos.
    """
    receitas = despesas = aportes = 0.0
    for t in transacoes:
        tx_type = t.get("type", 0)
        amount = t.get("amount", 0)
        if tx_type == 3:
            aportes += abs(amount)
        elif tx_type == 2 or (tx_type not in (1, 3) and amount < 0):
            despesas += abs(amount)
        elif tx_type == 1 or (tx_type not in (2, 3) and amount > 0):
            receitas += amount
    return receitas, despesas, aportes

def _listar_maiores_despesas(transacoes: list, limite: int = 8) -> list:
    """Retorna as N maiores despesas do mês para que o LLM analise a essencialidade."""
    despesas = []
    for t in transacoes:
        if t.get("type") == 2 or t.get("amount", 0) < 0:
            despesas.append({
                "descricao": t.get("description", "Despesa não identificada"),
                "categoria": str(t.get("categoryName", "Outros")).title(),
                "valor_gasto": round(abs(t.get("amount", 0)), 2)
            })
    return sorted(despesas, key=lambda x: x["valor_gasto"], reverse=True)[:limite]

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
        except httpx.RequestError as e:
            _logger.warning("🔌 [API] Backend .NET inacessível ao listar contas: %s", e)
            return _ERR_OFFLINE

        if r.status_code == 401:
            _logger.warning("🔑 [API] JWT rejeitado (401) ao listar contas")
            return _ERR_SESSAO
        if r.status_code != 200:
            _logger.warning("⚠️  [API] /accounts respondeu status %d", r.status_code)
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
        falhas = 0
        for grupo in grupos:
            if isinstance(grupo, list):
                todas.extend(grupo)
            else:
                falhas += 1

        _logger.info(
            "🔎 [API] %d transação(ões) agregadas de %d conta(s)%s",
            len(todas),
            len(ids),
            f" — {falhas} conta(s) falharam" if falhas else "",
        )
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
            if r.status_code == 401:
                return _ERR_SESSAO
            if r.status_code != 200:
                return f"Erro ao consultar contas (status {r.status_code})."

            contas = r.json() or []
            if not contas:
                return "O usuário ainda não possui contas cadastradas."

            # Saída compacta: só o que o LLM precisa (nome, tipo, saldo e o id
            # para ações como realizar_aporte_meta). JSON cru inflaria o contexto.
            linhas = [f"🏦 {len(contas)} conta(s):"]
            total = 0.0
            for c in contas:
                saldo = c.get("currentBalance", 0.0)
                total += saldo
                linhas.append(
                    f"  • {c.get('name', 'Sem nome')} ({c.get('typeName', 'Conta')}): "
                    f"R$ {saldo:,.2f} | id={c.get('id', '?')}"
                )
            linhas.append(f"  💰 Saldo total: R$ {total:,.2f}")
            _logger.info("🏦 [TOOL:saldos] %d conta(s) | saldo total R$ %.2f", len(contas), total)
            return "\n".join(linhas)
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:saldos] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:saldos] Erro inesperado: %s", e)
            return f"Erro inesperado ao consultar contas: {e}"

    @tool
    async def consultar_metas_financeiras() -> str:
        """Use esta ferramenta para verificar as metas financeiras do usuário
        (ex: comprar carro, fundo de emergência), ver o progresso, valores alvo e se a meta foi concluída."""
        try:
            r = await _get("/financial-goals")
            if r.status_code == 401:
                return _ERR_SESSAO
            if r.status_code != 200:
                return f"Erro ao consultar metas (status {r.status_code})."

            metas = r.json() or []
            if not metas:
                return "O usuário ainda não possui metas financeiras cadastradas."

            # Saída compacta: nome, progresso, prazo, status e id (necessário
            # para realizar_aporte_meta). JSON cru inflaria o contexto.
            linhas = [f"🎯 {len(metas)} meta(s):"]
            for m in metas:
                atual = m.get("currentAmount", 0.0)
                alvo = m.get("targetAmount", 0.0)
                pct = m.get("progressPercentage") or (atual / alvo * 100 if alvo else 0.0)
                status = "✅ concluída" if m.get("isCompleted") else "em andamento"
                prazo = (m.get("deadline") or "")[:10]
                linhas.append(
                    f"  • {m.get('name', '?')}: R$ {atual:,.2f} / R$ {alvo:,.2f} "
                    f"({pct:.1f}%) | prazo {prazo} | {status} | id={m.get('id', '?')}"
                )
            _logger.info("🎯 [TOOL:metas] %d meta(s) retornadas", len(metas))
            return "\n".join(linhas)
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:metas] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:metas] Erro inesperado: %s", e)
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
                sinal = "+" if amount > 0 else "-" if amount < 0 else ""
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

            _logger.info(
                "📋 [TOOL:extrato] %d/%d transação(ões) exibidas | período: %s",
                len(exibidas), total, label,
            )
            return "\n".join(linhas)

        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:extrato] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:extrato] Erro inesperado: %s", e)
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
            _logger.info(
                "📊 [TOOL:gastos] %d categoria(s) | total R$ %.2f | período: %s",
                len(gastos), total_despesas, label,
            )
            return "\n".join(linhas)

        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:gastos] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:gastos] Erro inesperado: %s", e)
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
                    label_mes = f"{_MESES_PT[tx_date.month]}/{tx_date.year}"
                    data_fmt = tx_date.strftime("%d/%m")
                except Exception:
                    # Sem data válida: não reutiliza tx_date de iteração anterior
                    chave_mes = "desconhecido"
                    label_mes = "Data desconhecida"
                    data_fmt = "??"

                if chave_mes not in meses:
                    meses[chave_mes] = {"label": label_mes, "total": 0.0, "transacoes": []}
                valor = abs(amount)
                meses[chave_mes]["total"] += valor
                meses[chave_mes]["transacoes"].append({
                    "data": data_fmt,
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
            _logger.info(
                "📊 [TOOL:relatorio] filtro='%s' | %d mês(es) | total R$ %.2f",
                filtro_categoria, len(meses), total_geral,
            )
            return "\n".join(linhas)

        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:relatorio] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:relatorio] Erro inesperado: %s", e)
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

            # Saldo do período = receitas − despesas. Aportes NÃO entram: o
            # dinheiro aportado em meta continua sendo patrimônio do usuário
            # (apenas mudou de lugar). Sem esta separação, um mês saudável com
            # aporte alto aparecia como "negativo" — dado incorreto ao usuário.
            saldo_periodo = total_receitas - total_despesas
            sobra_apos_aportes = saldo_periodo - total_investimentos
            gasto_medio_diario = total_despesas / dias_periodo
            taxa_poupanca = (total_investimentos / total_receitas * 100) if total_receitas > 0 else 0.0
            situacao = "✅ positivo" if saldo_periodo >= 0 else "❌ negativo"

            if gastos_categoria:
                cat_vila = max(gastos_categoria, key=lambda k: gastos_categoria[k])
                cat_vila_valor = gastos_categoria[cat_vila]
            else:
                cat_vila, cat_vila_valor = "N/A", 0.0

            # Texto em TÓPICOS curtos agrupados por bloco — mais intuitivo para
            # o usuário visualizar um resumo completo do que um parágrafo único.
            # Sem "##"/"###" (headers grandes): grupos em **negrito** e bullets
            # simples mantêm a hierarquia visual leve, e o CSS do frontend não
            # duplica espaçamento entre itens (bug de white-space já corrigido).
            linhas = [
                f"📊 Resumo financeiro — {label}",
                "",
                "**Fluxo de caixa**",
                f"• Receitas: R$ {total_receitas:,.2f}",
                f"• Despesas: R$ {total_despesas:,.2f}",
                f"• Saldo do período: R$ {saldo_periodo:,.2f} ({situacao})",
            ]
            if total_investimentos > 0:
                linhas.append(f"• Aportes em metas/investimentos: R$ {total_investimentos:,.2f}")
                linhas.append(f"• Sobra em conta após aportes: R$ {sobra_apos_aportes:,.2f}")

            linhas += [
                "",
                "**Indicadores**",
                f"• Gasto médio diário: R$ {gasto_medio_diario:,.2f}",
            ]
            if total_investimentos > 0:
                linhas.append(f"• Taxa de poupança (investido/receita): {taxa_poupanca:.1f}%")

            if cat_vila != "N/A":
                linhas += [
                    "",
                    "**Destaques**",
                    f"• Categoria que mais pesou: {cat_vila} (R$ {cat_vila_valor:,.2f})",
                    f"• Maior despesa única: {maior_despesa_desc} (R$ {maior_despesa_valor:,.2f})",
                ]

            _logger.info(
                "📋 [TOOL:resumo] receitas=R$ %.2f | despesas=R$ %.2f | invest=R$ %.2f | período: %s",
                total_receitas, total_despesas, total_investimentos, label,
            )
            return "\n".join(linhas)

        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:resumo] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:resumo] Erro inesperado: %s", e)
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
        _logger.info(
            "✍️  [TOOL:criar_meta] Solicitação: nome='%s' | alvo=R$ %.2f | prazo=%s",
            nome, valor_alvo, data_limite,
        )
        try:
            r = await _post("/financial-goals", payload)
            if r.status_code in (200, 201):
                _logger.info("✅ [TOOL:criar_meta] Meta '%s' criada com sucesso", nome)
                return (
                    f"✅ Meta criada com sucesso!\n"
                    f"  • Nome: {nome}\n"
                    f"  • Valor alvo: R$ {valor_alvo:,.2f}\n"
                    f"  • Prazo: {data_limite}"
                )
            if r.status_code == 401:
                _logger.warning("🔑 [TOOL:criar_meta] JWT rejeitado (401)")
                return _ERR_SESSAO
            if r.status_code == 400:
                _logger.warning("⚠️  [TOOL:criar_meta] Payload rejeitado (400): %s", r.text[:200])
                return f"Dados inválidos para criar a meta: {r.text}"
            _logger.warning("⚠️  [TOOL:criar_meta] Status inesperado: %d", r.status_code)
            return f"Erro ao criar meta (status {r.status_code})."
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:criar_meta] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:criar_meta] Erro inesperado: %s", e)
            return f"Erro inesperado ao criar meta: {e}"

    @tool
    async def realizar_aporte_meta(valor: float, goal_id: str, account_id: str) -> str:
        """Use esta ferramenta para investir ou guardar dinheiro em uma meta financeira específica.
        Recebe o valor, o ID da meta e o ID da conta de origem. Retorna sucesso ou erro."""
        _logger.info(
            "✍️  [TOOL:aporte] Solicitação: R$ %.2f | meta=%s | conta=%s",
            valor, goal_id[:8], account_id[:8],
        )
        try:
            r_cat = await _get("/categories")
            if r_cat.status_code != 200 or not r_cat.json():
                _logger.warning("⚠️  [TOOL:aporte] Nenhuma categoria disponível — aporte abortado")
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
                _logger.info("✅ [TOOL:aporte] R$ %.2f aportados na meta %s", valor, goal_id[:8])
                return f"✅ Aporte de R$ {valor:,.2f} realizado com sucesso na meta!"
            if r.status_code == 401:
                _logger.warning("🔑 [TOOL:aporte] JWT rejeitado (401)")
                return _ERR_SESSAO
            if r.status_code == 400:
                _logger.warning("⚠️  [TOOL:aporte] Payload rejeitado (400): %s", r.text[:200])
                return f"Dados inválidos: {r.text}"
            _logger.warning("⚠️  [TOOL:aporte] Status inesperado: %d", r.status_code)
            return f"Erro ao realizar aporte (status {r.status_code})."
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:aporte] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:aporte] Erro inesperado: %s", e)
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

            # Classificador único: aporte em meta NÃO é despesa, mas é dinheiro
            # comprometido — entra no cálculo de margem como item separado.
            total_receitas, despesas_atuais, aportes_metas = _classificar_fluxo(transacoes)
            compromissos_atuais = despesas_atuais + aportes_metas

            # Matemática determinística do cenário
            novo_total_compromissos = compromissos_atuais + valor_mensal
            saldo_livre_atual = total_receitas - compromissos_atuais
            novo_saldo_livre = total_receitas - novo_total_compromissos

            comprometimento_atual_pct = (compromissos_atuais / total_receitas * 100) if total_receitas > 0 else 0
            novo_comprometimento_pct = (novo_total_compromissos / total_receitas * 100) if total_receitas > 0 else 0

            # Classificação de Risco
            status_risco = "SEGURO"
            if novo_saldo_livre < 0:
                status_risco = "CRÍTICO - Orçamento ficará negativo"
            elif novo_comprometimento_pct > 80:
                status_risco = "ALTO RISCO - Restará pouca margem de segurança"

            _logger.info(
                "🧪 [TOOL:estresse] '%s' R$ %.2f/mês → %s (comprometimento %.1f%% → %.1f%%)",
                descricao_nova_despesa, valor_mensal, status_risco.split(" -")[0],
                comprometimento_atual_pct, novo_comprometimento_pct,
            )
            return {
                "analise": f"Impacto de assumir: {descricao_nova_despesa}",
                "cenario_atual": {
                    "receita_mensal": round(total_receitas, 2),
                    "despesa_mensal": round(despesas_atuais, 2),
                    "aportes_em_metas": round(aportes_metas, 2),
                    "saldo_livre": round(saldo_livre_atual, 2),
                    "comprometimento_renda_pct": round(comprometimento_atual_pct, 1)
                },
                "cenario_simulado": {
                    "nova_despesa": round(valor_mensal, 2),
                    "novo_total_compromissos": round(novo_total_compromissos, 2),
                    "novo_saldo_livre": round(novo_saldo_livre, 2),
                    "novo_comprometimento_renda_pct": round(novo_comprometimento_pct, 1)
                },
                "status_de_risco": status_risco
            }
            
        except Exception as e:
            _logger.error("❌ [TOOL:estresse] Erro inesperado: %s", e)
            return {"erro": f"Falha ao simular cenário: {str(e)}"}
        
    @tool(args_schema=SimularMetaIdealInput)
    async def simular_meta_ideal(objetivo_principal: str, valor_alvo_estimado: float = None, prazo_meses_desejado: int = None) -> dict:
        """
        Use esta ferramenta ANTES de criar uma meta quando o utilizador pedir ajuda para se organizar.
        Ela analisa o fluxo de caixa e devolve uma proposta matemática viável ou oportunidades de corte.
        """
        try:
            dt_fim = datetime.now(timezone.utc)
            dt_inicio = dt_fim - timedelta(days=30)
            
            transacoes = await _buscar_todas_transacoes(dt_inicio=dt_inicio, dt_fim=dt_fim)
            
            if isinstance(transacoes, str):
                return {"erro": transacoes}

            # Classificador único: aportes já feitos são compromisso (reduzem a
            # sobra disponível para uma NOVA meta), mas não são "despesa".
            total_receitas, despesas_atuais, aportes_metas = _classificar_fluxo(transacoes)
            saldo_livre = total_receitas - despesas_atuais - aportes_metas

            if saldo_livre <= 0:
                # Mantém a lógica de corte de despesas semântica que já fizemos
                _logger.info(
                    "🧮 [TOOL:meta_ideal] '%s' → orçamento negativo (rombo R$ %.2f) — sugerindo cortes",
                    objetivo_principal, abs(saldo_livre),
                )
                maiores_despesas = _listar_maiores_despesas(transacoes, limite=8)
                return {
                    "analise": "O orçamento atual está negativo ou zerado. Impossível criar meta.",
                    "cenario_atual": {
                        "receita_mensal": round(total_receitas, 2),
                        "despesa_mensal": round(despesas_atuais, 2),
                        "rombo_mensal": round(abs(saldo_livre), 2)
                    },
                    "maiores_despesas_do_mes": maiores_despesas,
                    "instrucao_ao_agente": (
                        "NÃO crie a meta. Analise a lista 'maiores_despesas_do_mes' usando a sua capacidade "
                        "de julgamento semântico. Identifique quais itens NÃO SÃO de sobrevivência básica. "
                        "Apresente-os ao utilizador e sugira um plano de corte focado neles para sair do vermelho."
                    )
                }

            # Capacidade de poupança conservadora (poupando toda a sobra ou metade dela)
            capacidade_maxima = saldo_livre
            aporte_sugerido = round(saldo_livre * 0.5, 2) 

            # === ÁRVORE DE DECISÃO DINÂMICA ===
            
            # Cenário 1: O utilizador sabe o valor E o prazo (Ex: "Quero 100k para um apartamento em 3 anos")
            if valor_alvo_estimado and prazo_meses_desejado:
                aporte_necessario = round(valor_alvo_estimado / prazo_meses_desejado, 2)
                
                if aporte_necessario > capacidade_maxima:
                    status = "INVIÁVEL NO ORÇAMENTO ATUAL"
                    instrucao = f"O aporte exigido (R$ {aporte_necessario}) é maior que a sobra mensal do utilizador (R$ {saldo_livre}). Sugira que ele estique o prazo ou que corte agressivamente as despesas."
                else:
                    status = "VIÁVEL"
                    instrucao = "O valor cabe no orçamento. Valide se ele quer criar a meta com este prazo e valor."
                    aporte_sugerido = aporte_necessario
                    
                valor_alvo = valor_alvo_estimado
                prazo_meses = prazo_meses_desejado

            # Cenário 2: O utilizador sabe o valor, mas não sabe quando consegue (Ex: "Preciso de 50k para um carro")
            elif valor_alvo_estimado:
                valor_alvo = valor_alvo_estimado
                # Calculamos o prazo com base no aporte sugerido (50% da sobra)
                prazo_meses = math.ceil(valor_alvo / aporte_sugerido) if aporte_sugerido > 0 else 0
                status = "CÁLCULO DE PRAZO"
                instrucao = f"Com base na sobra de caixa, calculei que levará {prazo_meses} meses poupando R$ {aporte_sugerido}/mês. Pergunte se o prazo o agrada."

            # Cenário 3: O utilizador sabe o prazo, mas quer saber quanto junta (Ex: "Vou casar daqui a 12 meses")
            elif prazo_meses_desejado:
                prazo_meses = prazo_meses_desejado
                valor_alvo = round(aporte_sugerido * prazo_meses, 2)
                status = "CÁLCULO DE POTENCIAL"
                instrucao = f"Poupando R$ {aporte_sugerido} por mês de forma segura, ele pode acumular R$ {valor_alvo} no prazo desejado. Apresente essa estimativa."

            # Cenário 4: O utilizador está perdido (A lógica clássica)
            else:
                if "reserva" in objetivo_principal.lower() or "emergência" in objetivo_principal.lower():
                    valor_alvo = round(despesas_atuais * 6, 2)
                else:
                    valor_alvo = round(aporte_sugerido * 12, 2)
                prazo_meses = math.ceil(valor_alvo / aporte_sugerido) if aporte_sugerido > 0 else 0
                status = "SUGESTÃO DIRECIONADA"
                instrucao = "O utilizador estava indeciso. Apresente o valor alvo sugerido e o prazo como um plano de ação ideal e pergunte se ele concorda."

            _logger.info(
                "🧮 [TOOL:meta_ideal] '%s' → %s | alvo=R$ %.2f | aporte=R$ %.2f/mês | %d meses",
                objetivo_principal, status, valor_alvo, aporte_sugerido, prazo_meses,
            )
            return {
                "objetivo": objetivo_principal,
                "status_simulacao": status,
                "cenario_atual": {
                    "sobra_mensal": round(saldo_livre, 2),
                    "aportes_ja_comprometidos": round(aportes_metas, 2)
                },
                "proposta_meta": {
                    "valor_total_alvo": valor_alvo,
                    "aporte_mensal_sugerido": aporte_sugerido,
                    "prazo_estimado_meses": prazo_meses
                },
                "instrucao_ao_agente": instrucao
            }

        except Exception as e:
            _logger.error("❌ [TOOL:meta_ideal] Erro inesperado: %s", e)
            return {"erro": f"Falha ao simular meta ideal: {str(e)}"}

    # =========================================================================
    # Ferramentas de perfil e investimentos (Agente Proativo)
    # =========================================================================

    @tool
    async def consultar_perfil_usuario() -> str:
        """Use esta ferramenta para consultar os dados de perfil do usuário logado,
        incluindo nome, e-mail e a renda mensal (salário) cadastrada. Necessária para
        qualquer cálculo que dependa da renda do usuário (ex: reserva de emergência)."""
        try:
            r = await _get("/Profile")
            if r.status_code == 401:
                return _ERR_SESSAO
            if r.status_code != 200:
                return f"Erro ao consultar perfil (status {r.status_code})."

            perfil = r.json() or {}
            renda = perfil.get("monthlyIncome")
            linhas = [f"👤 Perfil de {perfil.get('name', 'usuário')}:"]
            if renda is not None:
                linhas.append(f"  • Renda mensal cadastrada: R$ {renda:,.2f}")
            else:
                linhas.append("  • Renda mensal não cadastrada.")
            _logger.info("👤 [TOOL:perfil] renda=%s", renda)
            return "\n".join(linhas)
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:perfil] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:perfil] Erro inesperado: %s", e)
            return f"Erro inesperado ao consultar perfil: {e}"

    @tool
    async def consultar_investimentos() -> str:
        """Use esta ferramenta para listar os investimentos do usuário (Renda Fixa,
        Ação, FII, Cripto, ETF), com valor investido, valor atual e rentabilidade."""
        try:
            r = await _get("/investimentos")
            if r.status_code == 401:
                return _ERR_SESSAO
            if r.status_code != 200:
                return f"Erro ao consultar investimentos (status {r.status_code})."

            investimentos = r.json() or []
            if not investimentos:
                return "O usuário ainda não possui investimentos cadastrados."

            linhas = [f"📈 {len(investimentos)} investimento(s):"]
            for i in investimentos:
                tipo = _TIPO_INVESTIMENTO.get(i.get("tipo"), str(i.get("tipo", "?")))
                linhas.append(
                    f"  • {i.get('nome', '?')} ({tipo}): R$ {i.get('valorAtual', 0.0):,.2f} "
                    f"(investido R$ {i.get('valorInicial', 0.0):,.2f}, "
                    f"rentabilidade {i.get('rentabilidadePercentual', 0.0):.1f}%)"
                )
            _logger.info("📈 [TOOL:investimentos] %d investimento(s) retornados", len(investimentos))
            return "\n".join(linhas)
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:investimentos] API offline: %s", e)
            return _ERR_OFFLINE
        except Exception as e:
            _logger.error("❌ [TOOL:investimentos] Erro inesperado: %s", e)
            return f"Erro inesperado ao consultar investimentos: {e}"

    @tool
    async def analisar_reserva_emergencia() -> dict:
        """Use esta ferramenta para diagnosticar se o usuário possui uma reserva de
        emergência adequada. Ela busca a renda mensal do perfil, soma o valor guardado
        em metas financeiras com 'reserva' no nome e em investimentos de Renda Fixa,
        e calcula se esse total atinge o ideal de 6x a renda mensal. Retorna os números
        prontos — NÃO calcule por conta própria, apenas use este resultado."""
        try:
            r_perfil, r_metas, r_investimentos = await asyncio.gather(
                _get("/Profile"), _get("/financial-goals"), _get("/investimentos"),
            )

            if 401 in (r_perfil.status_code, r_metas.status_code, r_investimentos.status_code):
                return {"erro": _ERR_SESSAO}

            perfil = r_perfil.json() if r_perfil.status_code == 200 else {}
            renda_mensal = (perfil or {}).get("monthlyIncome") or 0.0

            if not renda_mensal:
                return {
                    "erro": (
                        "O usuário não possui renda mensal cadastrada no perfil. "
                        "Não é possível calcular a reserva ideal sem essa informação."
                    )
                }

            metas = r_metas.json() if r_metas.status_code == 200 else []
            metas_reserva = [
                m for m in (metas or [])
                if "reserva" in (m.get("name") or "").lower()
            ]
            valor_em_metas_reserva = sum(m.get("currentAmount", 0.0) for m in metas_reserva)
            possui_meta_reserva = len(metas_reserva) > 0

            investimentos = r_investimentos.json() if r_investimentos.status_code == 200 else []
            investimentos_renda_fixa = [
                i for i in (investimentos or []) if i.get("tipo") == _TIPO_RENDA_FIXA
            ]
            valor_em_renda_fixa = sum(i.get("valorAtual", 0.0) for i in investimentos_renda_fixa)
            possui_investimento_renda_fixa = len(investimentos_renda_fixa) > 0

            valor_ideal = round(renda_mensal * 6, 2)
            valor_atual = round(valor_em_metas_reserva + valor_em_renda_fixa, 2)
            percentual_atingido = round(valor_atual / valor_ideal * 100, 1) if valor_ideal > 0 else 0.0
            meses_cobertos = round(valor_atual / renda_mensal, 1) if renda_mensal > 0 else 0.0
            reserva_adequada = valor_atual >= valor_ideal
            valor_faltante = round(max(valor_ideal - valor_atual, 0.0), 2)

            _logger.info(
                "🛡️  [TOOL:reserva] renda=R$ %.2f | ideal=R$ %.2f | atual=R$ %.2f | adequada=%s",
                renda_mensal, valor_ideal, valor_atual, reserva_adequada,
            )
            return {
                "renda_mensal": round(renda_mensal, 2),
                "valor_ideal_reserva": valor_ideal,
                "valor_atual_guardado": valor_atual,
                "detalhamento": {
                    "em_metas_reserva": round(valor_em_metas_reserva, 2),
                    "em_investimentos_renda_fixa": round(valor_em_renda_fixa, 2),
                },
                "meses_de_despesa_cobertos": meses_cobertos,
                "percentual_atingido": percentual_atingido,
                "reserva_adequada": reserva_adequada,
                "valor_faltante": valor_faltante,
                "possui_meta_reserva": possui_meta_reserva,
                "possui_investimento_renda_fixa": possui_investimento_renda_fixa,
            }
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:reserva] API offline: %s", e)
            return {"erro": _ERR_OFFLINE}
        except Exception as e:
            _logger.error("❌ [TOOL:reserva] Erro inesperado: %s", e)
            return {"erro": f"Falha ao analisar reserva de emergência: {e}"}

    @tool
    async def analisar_inflacao_estilo_vida() -> dict:
        """Use esta ferramenta para diagnosticar 'inflação de estilo de vida': gastos
        supérfluos (lazer, restaurantes, assinaturas, delivery) crescendo no mesmo ritmo
        ou mais rápido que a renda, sem um aumento correspondente nos investimentos.
        Analisa os últimos 6 meses de transações comparando o trimestre mais recente
        com o anterior. Retorna os números prontos — NÃO calcule por conta própria."""
        try:
            agora = datetime.now(timezone.utc)
            dt_inicio_6m = agora - timedelta(days=180)
            dt_corte_3m = agora - timedelta(days=90)

            r_perfil, transacoes = await asyncio.gather(
                _get("/Profile"),
                _buscar_todas_transacoes(dt_inicio_6m, agora),
            )

            if isinstance(transacoes, str):
                return {"erro": transacoes}

            renda_cadastrada = (r_perfil.json() if r_perfil.status_code == 200 else {}).get(
                "monthlyIncome"
            ) or 0.0

            def _e_estilo_de_vida(t: dict) -> bool:
                texto = f"{t.get('categoryName') or t.get('category') or ''} {t.get('description') or ''}".lower()
                return any(kw in texto for kw in _KEYWORDS_ESTILO_DE_VIDA)

            def _parse_data(t: dict) -> datetime:
                try:
                    d = datetime.fromisoformat((t.get("date") or "").replace("Z", "+00:00"))
                    return d if d.tzinfo else d.replace(tzinfo=timezone.utc)
                except Exception:
                    return agora

            recentes = [t for t in transacoes if _parse_data(t) >= dt_corte_3m]
            anteriores = [t for t in transacoes if _parse_data(t) < dt_corte_3m]

            def _resumo_periodo(grupo: list) -> dict:
                receitas, _despesas, aportes = _classificar_fluxo(grupo)
                estilo_vida = sum(
                    abs(t.get("amount", 0))
                    for t in grupo
                    if (t.get("type") == 2 or t.get("amount", 0) < 0) and _e_estilo_de_vida(t)
                )
                return {"receitas": receitas, "aportes": aportes, "estilo_vida": estilo_vida}

            atual = _resumo_periodo(recentes)
            anterior = _resumo_periodo(anteriores)

            def _variacao_pct(novo: float, antigo: float) -> float | None:
                if antigo <= 0:
                    return None if novo <= 0 else 100.0
                return round((novo - antigo) / antigo * 100, 1)

            variacao_renda = _variacao_pct(atual["receitas"], anterior["receitas"])
            variacao_estilo_vida = _variacao_pct(atual["estilo_vida"], anterior["estilo_vida"])
            variacao_aportes = _variacao_pct(atual["aportes"], anterior["aportes"])

            media_mensal_estilo_vida = round(atual["estilo_vida"] / 3, 2)
            percentual_da_renda = (
                round(media_mensal_estilo_vida / renda_cadastrada * 100, 1)
                if renda_cadastrada > 0
                else None
            )

            dados_suficientes = len(anteriores) > 0

            if dados_suficientes:
                alerta = bool(
                    (variacao_estilo_vida is not None and variacao_aportes is not None
                     and variacao_estilo_vida > variacao_aportes)
                    or (variacao_renda is not None and variacao_renda > 5 and (variacao_aportes or 0) <= 0)
                    or (percentual_da_renda is not None and percentual_da_renda > 30)
                )
            else:
                alerta = bool(percentual_da_renda is not None and percentual_da_renda > 30)

            _logger.info(
                "📈 [TOOL:inflacao] estilo_vida=R$ %.2f/mês (%.1f%% renda) | var_estilo=%s | var_aportes=%s | alerta=%s",
                media_mensal_estilo_vida, percentual_da_renda or 0.0,
                variacao_estilo_vida, variacao_aportes, alerta,
            )
            return {
                "renda_mensal_cadastrada": round(renda_cadastrada, 2),
                "gasto_estilo_vida_ultimo_trimestre": round(atual["estilo_vida"], 2),
                "gasto_estilo_vida_trimestre_anterior": round(anterior["estilo_vida"], 2),
                "media_mensal_estilo_vida": media_mensal_estilo_vida,
                "percentual_da_renda_em_estilo_vida": percentual_da_renda,
                "variacao_renda_pct": variacao_renda,
                "variacao_estilo_vida_pct": variacao_estilo_vida,
                "variacao_aportes_pct": variacao_aportes,
                "dados_suficientes": dados_suficientes,
                "alerta_inflacao_estilo_vida": alerta,
            }
        except httpx.RequestError as e:
            _logger.warning("🔌 [TOOL:inflacao] API offline: %s", e)
            return {"erro": _ERR_OFFLINE}
        except Exception as e:
            _logger.error("❌ [TOOL:inflacao] Erro inesperado: %s", e)
            return {"erro": f"Falha ao analisar inflação de estilo de vida: {e}"}

    return [
        consultar_saldos_contas,
        consultar_metas_financeiras,
        consultar_transacoes_recentes,
        criar_meta_financeira,
        realizar_aporte_meta,
        analisar_gastos_por_categoria,
        relatorio_mensal_por_categoria,
        calcular_resumo_financeiro,
        simular_impacto_nova_despesa,
        simular_meta_ideal,
        consultar_perfil_usuario,
        consultar_investimentos,
        analisar_reserva_emergencia,
        analisar_inflacao_estilo_vida,
    ]
