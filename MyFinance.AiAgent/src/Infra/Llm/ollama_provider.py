import logging
import os
import time
from typing import List
import requests
from langchain_core.embeddings import Embeddings
from dotenv import load_dotenv

load_dotenv()

_logger = logging.getLogger("myfinance.agent")

# ── Endpoints ─────────────────────────────────────────────────────────────────
_REMOTE_URL = os.getenv("REMOTE_OLLAMA_URL", "http://ollama.futurelab.dcc.ufmg.br")
_REMOTE_API_KEY = os.getenv("REMOTE_OLLAMA_API_KEY", "pedrobarrosnvk58n6f")
_LOCAL_URL = os.getenv("LOCAL_OLLAMA_URL", "http://localhost:11434")

# ── Modelos por papel e por provedor ──────────────────────────────────────────
# Remoto não tem gemma4 — usa qwen2.5:7b como substituto para chat.
_MODELS: dict[str, dict[str, str]] = {
    "chat": {
        "local":  os.getenv("CHAT_MODEL_LOCAL",  "gemma4:latest"),
        "remote": os.getenv("CHAT_MODEL_REMOTE",  "llama3.1:8b"),
    },
    "classifier": {
        "local":  os.getenv("CLASSIFIER_MODEL_LOCAL",  "llama3.2:3b"),
        "remote": os.getenv("CLASSIFIER_MODEL_REMOTE", "llama3.2:3b"),
    },
    "embedding": {
        "local":  os.getenv("EMBEDDING_MODEL_LOCAL",  "nomic-embed-text"),
        "remote": os.getenv("EMBEDDING_MODEL_REMOTE", "nomic-embed-text:latest"),
    },
    # Modelos que lidam bem com JSON estruturado para o SemanticExtractor
    "extractor": {
        "local":  os.getenv("EXTRACTOR_MODEL_LOCAL",  "qwen2.5:7b"),
        "remote": os.getenv("EXTRACTOR_MODEL_REMOTE", "llama3.2:3b"),
    },
}

# ── Cache do health check (TTL = 60 s) ────────────────────────────────────────
_CACHE_TTL = 60.0
_last_ts: float = 0.0
_remote_ok: bool = False


def _check_remote() -> bool:
    """Health check contra a API remota, com resultado cacheado por _CACHE_TTL segundos."""
    global _last_ts, _remote_ok

    now = time.monotonic()
    if now - _last_ts < _CACHE_TTL:
        return _remote_ok

    try:
        r = requests.get(f"{_REMOTE_URL}/health", timeout=5)
        _remote_ok = r.status_code == 200
    except Exception:
        _remote_ok = False

    _last_ts = now

    if _remote_ok:
        _logger.info("🌐 [PROVIDER] API remota disponível — %s", _REMOTE_URL)
    else:
        _logger.warning("⚠️  [PROVIDER] API remota indisponível — usando Ollama local (%s)", _LOCAL_URL)

    return _remote_ok


def is_remote() -> bool:
    """Retorna True se a API remota está sendo usada."""
    return _check_remote()


def get_ollama_config() -> dict:
    """
    Retorna {'base_url': ..., 'client_kwargs': {...}} compatível com
    ChatOllama, OllamaLLM e OllamaEmbeddings do langchain-ollama.
    Prioriza a API remota; cai para o Ollama local se o health check falhar.
    """
    if _check_remote():
        return {
            "base_url": _REMOTE_URL,
            "client_kwargs": {"headers": {"X-API-Key": _REMOTE_API_KEY}},
        }
    return {
        "base_url": _LOCAL_URL,
        "client_kwargs": {},
    }


def get_model(role: str) -> str:
    """
    Retorna o nome do modelo para o papel indicado ('chat', 'classifier', 'embedding')
    de acordo com o provedor ativo.
    """
    provider = "remote" if _check_remote() else "local"
    model = _MODELS.get(role, {}).get(provider)
    if not model:
        raise ValueError(f"Papel '{role}' ou provedor '{provider}' não configurado.")
    return model


# ── Embeddings ─────────────────────────────────────────────────────────────────

class _RemoteEmbeddings(Embeddings):
    """
    Embeddings para o proxy da disciplina.
    O proxy expõe POST /api/embeddings (formato legado Ollama).
    langchain-ollama usa /api/embed (novo), causando 404 — por isso usamos HTTP direto.
    """

    def __init__(self, model: str, base_url: str, api_key: str) -> None:
        self.model = model
        self.base_url = base_url
        self.api_key = api_key

    def _embed_one(self, text: str) -> List[float]:
        r = requests.post(
            f"{self.base_url}/api/embeddings",
            headers={"X-API-Key": self.api_key, "Content-Type": "application/json"},
            json={"model": self.model, "prompt": text},
            timeout=30,
        )
        r.raise_for_status()
        return r.json()["embedding"]

    def embed_documents(self, texts: List[str]) -> List[List[float]]:
        return [self._embed_one(t) for t in texts]

    def embed_query(self, text: str) -> List[float]:
        return self._embed_one(text)


def get_embeddings() -> Embeddings:
    """
    Retorna a instância de embeddings correta para o provedor ativo:
    - Remoto: _RemoteEmbeddings (chama /api/embeddings com X-API-Key)
    - Local: OllamaEmbeddings (usa ollama.Client → /api/embed)
    """
    from langchain_ollama import OllamaEmbeddings

    model = get_model("embedding")
    if _check_remote():
        return _RemoteEmbeddings(model=model, base_url=_REMOTE_URL, api_key=_REMOTE_API_KEY)
    return OllamaEmbeddings(model=model, base_url=_LOCAL_URL)
