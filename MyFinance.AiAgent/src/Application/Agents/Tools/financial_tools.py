from langchain_core.tools import tool
from src.Infra.Data.financial_rag import FinancialKnowledgeBase

# Singleton — o índice FAISS é carregado uma única vez e reutilizado
_kb = FinancialKnowledgeBase()


@tool
def consultar_teoria_financeira(query: str) -> str:
    """Use esta ferramenta SEMPRE que precisar dar um conselho financeiro, sugerir cortes de gastos,
    falar sobre investimentos, ativos vs passivos, independência financeira, orçamento pessoal,
    regra dos 50/30/20, juros compostos, reserva de emergência, ou qualquer princípio e regra
    financeira consagrada. Recebe uma dúvida ou contexto do usuário e devolve trechos relevantes
    extraídos de livros de finanças pessoais."""
    return _kb.search(query)
