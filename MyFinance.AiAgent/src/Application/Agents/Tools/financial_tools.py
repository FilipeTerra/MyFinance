import logging
from langchain_core.tools import tool
from src.Infra.Data.financial_rag import FinancialKnowledgeBase

_logger = logging.getLogger("myfinance.agent")

# Singleton — o índice FAISS é carregado uma única vez e reutilizado
_kb = FinancialKnowledgeBase()


@tool
def consultar_teoria_financeira(query: str) -> str:
    """Use esta ferramenta SEMPRE que precisar dar um conselho financeiro, sugerir cortes de gastos,
    falar sobre investimentos, ativos vs passivos, independência financeira, orçamento pessoal,
    regra dos 50/30/20, juros compostos, reserva de emergência, ou qualquer princípio e regra
    financeira consagrada. Recebe uma dúvida ou contexto do usuário e devolve trechos relevantes
    extraídos de livros de finanças pessoais."""
    _logger.info("📚 [RAG]  Consultando base de conhecimento: '%s'", query[:80])
    result = _kb.search(query)
    if "não inicializada" in result:
        _logger.warning("⚠️  [RAG]  Índice FAISS ausente. Adicione livros em data/books/ e chame POST /api/ai/ingest.")
        return result
    _logger.info("📚 [RAG]  %d chars retornados da base de conhecimento.", len(result))
    return (
        "=== TRECHOS EXTRAÍDOS DOS LIVROS DE FINANÇAS PESSOAIS ===\n\n"
        f"{result}\n\n"
        "=== FIM DOS TRECHOS ===\n"
        "INSTRUÇÃO: Apresente os conceitos acima ao usuário. Cite os pontos principais "
        "que os livros ensinam sobre o tema perguntado."
    )
