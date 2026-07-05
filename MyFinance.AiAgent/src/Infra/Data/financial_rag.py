import os
import logging
from functools import lru_cache
from langchain_core.embeddings import Embeddings
from langchain_core.documents import Document
from langchain_community.vectorstores import FAISS
from langchain_community.document_loaders import DirectoryLoader, PyPDFLoader, TextLoader
from langchain_text_splitters import RecursiveCharacterTextSplitter, MarkdownHeaderTextSplitter
from src.Infra.Config.settings import get_settings
from src.Infra.Llm.ollama_provider import get_embeddings

_INDEX_PATH = get_settings().faiss_index_path

# Chunking — os livros são densos: cada seção "##" tem parágrafos de 500-900
# chars. O antigo chunk_size=600 cortava parágrafos no meio, fragmentando ideias
# entre dois vetores. 1200/150 mantém cada ideia inteira, com sobreposição de
# borda suficiente para não perder o contexto na fronteira.
_CHUNK_SIZE = 1200
_CHUNK_OVERLAP = 150

# Cabeçalhos markdown "##" são fronteiras semânticas naturais nos livros: viram
# limite de chunk E metadado ("secao"), usado depois para citar a fonte.
_MD_HEADERS = [("##", "secao")]

# Slug do arquivo-fonte → título legível, para o agente citar de qual livro veio
# o trecho. Arquivos fora do mapa caem no fallback (slug → Title Case).
_BOOK_TITLES = {
    "pai-rico-pai-pobre": "Pai Rico, Pai Pobre",
    "o-homem-mais-rico-da-babilonia": "O Homem Mais Rico da Babilônia",
    "ensinamentos-financas": "Ensinamentos de Finanças Pessoais",
}


def _titulo_livro(source: str) -> str:
    """Deriva um título legível a partir do caminho do arquivo-fonte."""
    stem = os.path.splitext(os.path.basename(source))[0]
    return _BOOK_TITLES.get(stem, stem.replace("-", " ").replace("_", " ").title())


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

        Estratégia de chunking por tipo de arquivo:
          .txt (livros em markdown) → divide primeiro por seção "##", carregando
            o título da seção como metadado, e só então limita o tamanho. Assim
            cada chunk cobre uma ideia completa e sabe de qual seção/livro veio.
          .pdf → sem estrutura markdown confiável: divide apenas por caracteres.
        """
        txt_docs = DirectoryLoader(
            directory_path, glob="**/*.txt", loader_cls=TextLoader, silent_errors=True
        ).load()
        pdf_docs = DirectoryLoader(
            directory_path, glob="**/*.pdf", loader_cls=PyPDFLoader, silent_errors=True
        ).load()

        if not txt_docs and not pdf_docs:
            raise ValueError(f"Nenhum documento .txt ou .pdf encontrado em '{directory_path}'.")

        char_splitter = RecursiveCharacterTextSplitter(
            chunk_size=_CHUNK_SIZE, chunk_overlap=_CHUNK_OVERLAP
        )
        md_splitter = MarkdownHeaderTextSplitter(
            headers_to_split_on=_MD_HEADERS, strip_headers=False
        )

        chunks: list[Document] = []

        # .txt: quebra por seção "##" (preserva o título em metadata["secao"]),
        # propaga o arquivo-fonte e depois corta seções longas por caracteres.
        for doc in txt_docs:
            source = doc.metadata.get("source", "")
            secoes = md_splitter.split_text(doc.page_content)
            for sec in secoes:
                sec.metadata["source"] = source
            chunks.extend(char_splitter.split_documents(secoes))

        # .pdf: divisão puramente por caracteres.
        chunks.extend(char_splitter.split_documents(pdf_docs))

        embeddings = self._make_embeddings()
        os.makedirs(_INDEX_PATH, exist_ok=True)
        self._vectorstore = FAISS.from_documents(chunks, embeddings)
        self._vectorstore.save_local(_INDEX_PATH)

        return len(chunks)

    @staticmethod
    def _format_snippet(doc: Document) -> str:
        """Prefixa o trecho com a fonte (livro — seção) para o agente citar."""
        titulo = _titulo_livro(doc.metadata.get("source", ""))
        secao = doc.metadata.get("secao")
        fonte = f"{titulo} — {secao}" if secao else titulo
        return f"[Fonte: {fonte}]\n{doc.page_content}"

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
        return "\n\n---\n\n".join(self._format_snippet(doc) for doc in results)


@lru_cache
def get_financial_knowledge_base() -> FinancialKnowledgeBase:
    """
    Instância única (cacheada) da base de conhecimento RAG.

    Antes, financial_tools.py e lifestyle_monitor_agent.py criavam cada um o
    seu próprio singleton `FinancialKnowledgeBase()` — dois objetos distintos
    carregando o MESMO índice FAISS do disco de forma independente. Esta
    factory garante que todo o processo compartilhe uma única instância.
    """
    return FinancialKnowledgeBase()
