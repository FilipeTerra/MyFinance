import os
import requests
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
        """Use esta ferramenta para ver os gastos, despesas e receitas recentes do usuário,
        entender o seu comportamento financeiro ou identificar vazamentos de dinheiro.
        Retorna as transações das contas do usuário."""
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

    return [
        consultar_saldos_contas,
        consultar_metas_financeiras,
        consultar_transacoes_recentes,
        criar_meta_financeira,
    ]
