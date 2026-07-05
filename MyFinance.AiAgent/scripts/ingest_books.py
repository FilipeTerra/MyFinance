"""
ingest_books.py — Migration de ingestão da base de conhecimento (RAG).

Reprocessa os livros de data/books/ e regrava o índice FAISS em data/faiss_index/.

A ingestão é SOB DEMANDA, como uma migration: o servidor não reprocessa mais os
embeddings a cada subida (os livros são estáticos). Rode este script manualmente
quando adicionar ou editar um livro, e versione o índice FAISS resultante.

Uso (a partir de qualquer diretório):
    python scripts/ingest_books.py             # usa data/books
    python scripts/ingest_books.py <diretorio> # diretório alternativo
"""
import os
import sys

# Raiz do microsserviço (pai de scripts/). O índice e os livros são referenciados
# por caminhos relativos ("data/books", "data/faiss_index"), então garantimos que
# o script resolva-os igual ao servidor, rode ele de onde rodar.
_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(_ROOT)
sys.path.insert(0, _ROOT)

from src.Infra.Config.settings import get_settings  # noqa: E402
from src.Infra.Logging.agent_logger import setup_logging  # noqa: E402
from src.Infra.Data.financial_rag import FinancialKnowledgeBase  # noqa: E402


def main() -> int:
    setup_logging()
    directory = sys.argv[1] if len(sys.argv) > 1 else get_settings().books_dir

    kb = FinancialKnowledgeBase()
    try:
        total = kb.ingest_documents(directory)
    except ValueError as e:
        print(f"❌ {e}")
        return 1

    print(f"✅ {total} chunks indexados a partir de '{directory}' → data/faiss_index/")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
