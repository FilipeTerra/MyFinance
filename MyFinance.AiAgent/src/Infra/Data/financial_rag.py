import os
import logging
from langchain_core.embeddings import Embeddings
from langchain_community.vectorstores import FAISS
from langchain_community.document_loaders import DirectoryLoader, PyPDFLoader, TextLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter
from src.Infra.Llm.ollama_provider import get_embeddings

_INDEX_PATH = "data/faiss_index"


class FinancialKnowledgeBase:
    """Gerencia o índice vetorial FAISS com literatura financeira."""

    def __init__(self):
        self._vectorstore: FAISS | None = None
        if not os.path.exists(_INDEX_PATH):
            logging.getLogger("myfinance.agent").warning(
                "⚠️  [RAG]  Índice FAISS não encontrado em '%s'. "
                "Adicione livros em data/books/ e chame POST /api/ai/ingest para ativar o RAG.",
                _INDEX_PATH,
            )

    def _make_embeddings(self) -> Embeddings:
        """Retorna a instância de embeddings para o provedor ativo."""
        return get_embeddings()

    def _load_index(self):
        """Carrega o índice do disco na primeira chamada (lazy-load)."""
        if self._vectorstore is None and os.path.exists(_INDEX_PATH):
            self._vectorstore = FAISS.load_local(
                _INDEX_PATH,
                self._make_embeddings(),
                allow_dangerous_deserialization=True,
            )

    def ingest_documents(self, directory_path: str) -> int:
        """
        Lê .txt e .pdf de um diretório, gera embeddings e salva o índice FAISS.
        Retorna o número de chunks indexados.
        Cada chamada recria o índice do zero (idempotente).
        """
        txt_loader = DirectoryLoader(
            directory_path, glob="**/*.txt", loader_cls=TextLoader, silent_errors=True
        )
        pdf_loader = DirectoryLoader(
            directory_path, glob="**/*.pdf", loader_cls=PyPDFLoader, silent_errors=True
        )

        docs = txt_loader.load() + pdf_loader.load()
        if not docs:
            raise ValueError(f"Nenhum documento .txt ou .pdf encontrado em '{directory_path}'.")

        splitter = RecursiveCharacterTextSplitter(chunk_size=600, chunk_overlap=80)
        chunks = splitter.split_documents(docs)

        embeddings = self._make_embeddings()
        os.makedirs(_INDEX_PATH, exist_ok=True)
        self._vectorstore = FAISS.from_documents(chunks, embeddings)
        self._vectorstore.save_local(_INDEX_PATH)

        return len(chunks)

    def search(self, query: str, k: int = 3) -> str:
        """Busca os trechos mais relevantes para a query."""
        self._load_index()
        if self._vectorstore is None:
            return (
                "Base de conhecimento ainda não inicializada. "
                "Envie documentos para data/books/ e chame POST /api/ai/ingest."
            )
        # Atualiza a função de embedding para usar o provedor atual
        fresh = self._make_embeddings()
        self._vectorstore.embedding_function = fresh.embed_query
        results = self._vectorstore.similarity_search(query, k=k)
        return "\n\n---\n\n".join(doc.page_content for doc in results)
