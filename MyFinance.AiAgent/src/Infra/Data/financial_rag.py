import os
from langchain_ollama import OllamaEmbeddings
from langchain_community.vectorstores import FAISS
from langchain_community.document_loaders import DirectoryLoader, PyPDFLoader, TextLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter

# Modelo dedicado a embeddings — leve e eficiente
_EMBEDDING_MODEL = "nomic-embed-text"
_INDEX_PATH = "data/faiss_index"


class FinancialKnowledgeBase:
    """Gerencia o índice vetorial FAISS com literatura financeira."""

    def __init__(self):
        self.embeddings = OllamaEmbeddings(model=_EMBEDDING_MODEL)
        self._vectorstore: FAISS | None = None

    def _load_index(self):
        """Carrega o índice do disco na primeira chamada (lazy-load)."""
        if self._vectorstore is None and os.path.exists(_INDEX_PATH):
            self._vectorstore = FAISS.load_local(
                _INDEX_PATH,
                self.embeddings,
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

        os.makedirs(_INDEX_PATH, exist_ok=True)
        self._vectorstore = FAISS.from_documents(chunks, self.embeddings)
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
        results = self._vectorstore.similarity_search(query, k=k)
        return "\n\n---\n\n".join(doc.page_content for doc in results)
