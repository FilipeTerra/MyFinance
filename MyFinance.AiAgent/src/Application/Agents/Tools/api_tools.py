import os
import requests
from datetime import datetime, timedelta, timezone
from dotenv import load_dotenv
from langchain_core.tools import tool

load_dotenv()

_API_BASE_URL = os.getenv("API_URL", "http://localhost:5088/api")


def make_api_tools(jwt_token: str) -> list:
    """
    Factory que cria as ferramentas HTTP com o JWT baked-in via closure.
    O LLM nunca vê o token — ele apenas chama as ferramentas pelo nome.
    """
    _headers = {"Authorization": f"Bearer {jwt_token}"}

    @tool
    def consultar_saldos_contas() -> str:
        """Use esta ferramenta para verificar o saldo atual, listar as contas bancárias
        do usuário ou ver quanto dinheiro ele tem disponível. Retorna uma lista de contas e saldos."""
        try:
            response = requests.get(
                f"{_API_BASE_URL}/accounts", headers=_headers, timeout=10
            )
            if response.status_code == 200:
                contas = response.json()
                return contas if contas else "O usuário ainda não possui contas cadastradas."
            if response.status_code == 401:
                return "Sessão expirada. O usuário precisa fazer login novamente."
            return f"Erro ao consultar contas (status {response.status_code})."
        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao consultar contas: {e}"

    @tool
    def consultar_metas_financeiras() -> str:
        """Use esta ferramenta para verificar as metas financeiras do usuário
        (ex: comprar carro, fundo de emergência), ver o progresso, valores alvo e se a meta foi concluída."""
        try:
            response = requests.get(
                f"{_API_BASE_URL}/financial-goals", headers=_headers, timeout=10
            )
            if response.status_code == 200:
                metas = response.json()
                return metas if metas else "O usuário ainda não possui metas financeiras cadastradas."
            if response.status_code == 401:
                return "Sessão expirada. O usuário precisa fazer login novamente."
            return f"Erro ao consultar metas (status {response.status_code})."
        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao consultar metas: {e}"

    @tool
    def consultar_transacoes_recentes() -> str:
        """Use esta ferramenta APENAS para listar transações individuais recentes quando o usuário
        quiser ver o extrato ou histórico de movimentações específicas (ex: 'mostre minhas últimas transações',
        'o que comprei recentemente'). NÃO use para análise de gastos por categoria ou resumo financeiro —
        para isso use analisar_gastos_por_categoria e calcular_resumo_financeiro."""
        try:
            acc_response = requests.get(
                f"{_API_BASE_URL}/accounts", headers=_headers, timeout=10
            )
            if acc_response.status_code != 200:
                return "Não foi possível recuperar as contas do usuário para buscar transações."

            contas = acc_response.json()
            if not contas:
                return "O usuário não possui contas cadastradas para consultar transações."

            todas_transacoes = []
            for conta in contas:
                account_id = conta.get("id")
                if not account_id:
                    continue
                tx_response = requests.get(
                    f"{_API_BASE_URL}/transactions/account/{account_id}",
                    headers=_headers,
                    timeout=10,
                )
                if tx_response.status_code == 200:
                    todas_transacoes.extend(tx_response.json() or [])

            if not todas_transacoes:
                return "Nenhuma transação encontrada para as contas do usuário."
            return todas_transacoes

        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao consultar transações: {e}"

    @tool
    def criar_meta_financeira(nome: str, valor_alvo: float, data_limite: str) -> str:
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
            response = requests.post(
                f"{_API_BASE_URL}/financial-goals",
                json=payload,
                headers=_headers,
                timeout=10,
            )
            if response.status_code in (200, 201):
                return (
                    f"✅ Meta criada com sucesso!\n"
                    f"  • Nome: {nome}\n"
                    f"  • Valor alvo: R$ {valor_alvo:,.2f}\n"
                    f"  • Prazo: {data_limite}"
                )
            if response.status_code == 401:
                return "Sessão expirada. O usuário precisa fazer login novamente."
            if response.status_code == 400:
                return f"Dados inválidos para criar a meta: {response.text}"
            return f"Erro ao criar meta (status {response.status_code})."
        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao criar meta: {e}"

    def _buscar_todas_transacoes(ultimos_dias: int) -> list | str:
        """Busca todas as transações de todas as contas num janela de dias."""
        acc_response = requests.get(
            f"{_API_BASE_URL}/accounts", headers=_headers, timeout=10
        )
        if acc_response.status_code != 200:
            return "Não foi possível recuperar as contas do usuário."
        contas = acc_response.json()
        if not contas:
            return "O usuário não possui contas cadastradas."

        cutoff = datetime.now(timezone.utc) - timedelta(days=ultimos_dias)
        todas = []
        for conta in contas:
            account_id = conta.get("id")
            if not account_id:
                continue
            tx_response = requests.get(
                f"{_API_BASE_URL}/transactions/account/{account_id}",
                headers=_headers, timeout=10,
            )
            if tx_response.status_code == 200:
                for t in (tx_response.json() or []):
                    date_str = t.get("date", "")
                    try:
                        tx_date = datetime.fromisoformat(date_str.replace("Z", "+00:00"))
                        if tx_date >= cutoff:
                            todas.append(t)
                    except Exception:
                        todas.append(t)
        return todas

    @tool
    def analisar_gastos_por_categoria(ultimos_dias: int = 30) -> str:
        """Use esta ferramenta para analisar onde o usuário está gastando mais dinheiro,
        identificar padrões de consumo, responder 'onde gasto mais?', 'como melhorar meus gastos?'
        ou qualquer pergunta sobre categorias de despesas. Agrupa despesas por categoria
        e mostra os totais e percentuais. Parâmetro ultimos_dias define a janela de análise."""
        try:
            transacoes = _buscar_todas_transacoes(ultimos_dias)
            if isinstance(transacoes, str):
                return transacoes

            gastos: dict[str, float] = {}
            total_despesas = 0.0

            for t in transacoes:
                amount = t.get("amount", 0)
                if amount >= 0:
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
                return f"Nenhuma despesa encontrada nos últimos {ultimos_dias} dias."

            linhas = [f"📊 Gastos por categoria — últimos {ultimos_dias} dias\n"]
            for cat, total in sorted(gastos.items(), key=lambda x: x[1], reverse=True):
                pct = (total / total_despesas * 100) if total_despesas > 0 else 0
                linhas.append(f"  • {cat}: R$ {total:,.2f} ({pct:.1f}%)")
            linhas.append(f"\n  💸 Total gasto: R$ {total_despesas:,.2f}")
            return "\n".join(linhas)

        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao analisar gastos: {e}"

    @tool
    def relatorio_mensal_por_categoria(filtro_categoria: str, ultimos_meses: int = 3) -> str:
        """Use esta ferramenta quando o usuário quiser um relatório detalhado de gastos em uma
        categoria ou tipo de gasto específico (ex: 'transporte', 'uber', 'alimentação', 'lazer')
        quebrado mês a mês. Exemplos de uso: 'quanto gastei com uber nos últimos 3 meses?',
        'me dê um relatório mensal de transporte', 'quanto gastei com alimentação por mês?'.
        Parâmetros: filtro_categoria (palavra-chave para filtrar, ex: 'transporte', 'uber', 'alimentação'),
        ultimos_meses (número de meses a analisar, padrão 3)."""
        try:
            transacoes = _buscar_todas_transacoes(ultimos_meses * 31)
            if isinstance(transacoes, str):
                return transacoes

            filtro = filtro_categoria.lower().strip()

            meses: dict[str, dict] = {}
            for t in transacoes:
                amount = t.get("amount", 0)
                if amount >= 0:
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
                return f"Nenhuma despesa encontrada com o filtro '{filtro_categoria}' nos últimos {ultimos_meses} meses."

            total_geral = sum(m["total"] for m in meses.values())
            linhas = [f"📊 Relatório de '{filtro_categoria}' — últimos {ultimos_meses} meses\n"]

            for chave in sorted(meses.keys(), reverse=True):
                mes = meses[chave]
                linhas.append(f"\n📅 {mes['label']} — R$ {mes['total']:,.2f}")
                for tx in sorted(mes["transacoes"], key=lambda x: x["valor"], reverse=True):
                    linhas.append(f"  • {tx['data']} | {tx['descricao'][:40]} | {tx['categoria']} | R$ {tx['valor']:,.2f}")

            linhas.append(f"\n💸 Total no período: R$ {total_geral:,.2f}")
            linhas.append(f"📈 Média mensal: R$ {total_geral / len(meses):,.2f}")
            return "\n".join(linhas)

        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao gerar relatório: {e}"

    @tool
    def realizar_aporte_meta(valor: float, goal_id: str, account_id: str) -> str:
        """Use esta ferramenta para investir ou guardar dinheiro em uma meta financeira específica. Recebe o valor, o ID da meta e o ID da conta de origem. Retorna sucesso ou erro."""
        try:
            cat_response = requests.get(
                f"{_API_BASE_URL}/categories", headers=_headers, timeout=10
            )
            if cat_response.status_code != 200 or not cat_response.json():
                return "Erro: Nenhuma categoria encontrada. Crie pelo menos uma categoria antes de realizar um aporte."

            category_id = cat_response.json()[0]["id"]

            payload = {
                "amount": valor,
                "type": 3,
                "accountId": account_id,
                "financialGoalId": goal_id,
                "description": "Aporte na meta",
                "date": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S"),
                "categoryId": category_id,
            }
            response = requests.post(
                f"{_API_BASE_URL}/transactions",
                json=payload,
                headers=_headers,
                timeout=10,
            )
            if response.status_code in (200, 201):
                return f"✅ Aporte de R$ {valor:,.2f} realizado com sucesso na meta!"
            if response.status_code == 401:
                return "Sessão expirada. O usuário precisa fazer login novamente."
            if response.status_code == 400:
                return f"Dados inválidos: {response.text}"
            return f"Erro ao realizar aporte (status {response.status_code})."
        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao realizar aporte: {e}"

    @tool
    def calcular_resumo_financeiro(ultimos_dias: int = 30) -> str:
        """Use esta ferramenta para um resumo geral das finanças do usuário: total de receitas,
        total de despesas, saldo líquido e taxa de poupança no período. Use quando o usuário
        perguntar sobre saúde financeira, balanço do mês, situação financeira geral ou
        quanto está conseguindo poupar."""
        try:
            transacoes = _buscar_todas_transacoes(ultimos_dias)
            if isinstance(transacoes, str):
                return transacoes

            total_receitas = 0.0
            total_despesas = 0.0
            n = len(transacoes)

            for t in transacoes:
                amount = t.get("amount", 0)
                if amount > 0:
                    total_receitas += amount
                else:
                    total_despesas += abs(amount)

            saldo_liquido = total_receitas - total_despesas
            taxa_poupanca = (saldo_liquido / total_receitas * 100) if total_receitas > 0 else 0
            situacao = "✅ positivo" if saldo_liquido >= 0 else "❌ negativo"

            return (
                f"📋 Resumo financeiro — últimos {ultimos_dias} dias\n"
                f"  • Receitas:         R$ {total_receitas:,.2f}\n"
                f"  • Despesas:         R$ {total_despesas:,.2f}\n"
                f"  • Saldo líquido:    R$ {saldo_liquido:,.2f} ({situacao})\n"
                f"  • Taxa de poupança: {taxa_poupanca:.1f}%\n"
                f"  • Transações no período: {n}"
            )

        except requests.exceptions.ConnectionError:
            return "Erro: A API financeira está offline ou inacessível."
        except Exception as e:
            return f"Erro inesperado ao calcular resumo: {e}"

    return [
        consultar_saldos_contas,
        consultar_metas_financeiras,
        consultar_transacoes_recentes,
        criar_meta_financeira,
        realizar_aporte_meta,
        analisar_gastos_por_categoria,
        relatorio_mensal_por_categoria,
        calcular_resumo_financeiro,
    ]
