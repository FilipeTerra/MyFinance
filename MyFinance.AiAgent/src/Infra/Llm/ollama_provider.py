import logging
import time
from typing import List
import requests
from langchain_core.embeddings import Embeddings

from src.Infra.Config.settings import get_settings

_logger = logging.getLogger("myfinance.agent")
_settings = get_settings()

# ── Endpoints ─────────────────────────────────────────────────────────────────
_REMOTE_URL = _settings.remote_ollama_url
# A credencial vive APENAS no ambiente/.env (settings). Se ausente, o health
# check do remoto falha naturalmente e o sistema cai para o Ollama local.
_REMOTE_API_KEY = _settings.remote_ollama_api_key
_LOCAL_URL = _settings.local_ollama_url

if not _REMOTE_API_KEY:
    _logger.warning(
        "⚠️  [PROVIDER] REMOTE_OLLAMA_API_KEY não definida no .env — "
        "o provedor remoto será ignorado (fallback para Ollama local)."
    )

# ── Modelos por papel e por provedor ──────────────────────────────────────────
# Remoto não tem gemma4 — usa llama3.1:8b como substituto para chat.
_MODELS: dict[str, dict[str, str]] = {
    "chat":       {"local": _settings.chat_model_local,       "remote": _settings.chat_model_remote},
    "classifier": {"local": _settings.classifier_model_local, "remote": _settings.classifier_model_remote},
    "embedding":  {"local": _settings.embedding_model_local,  "remote": _settings.embedding_model_remote},
    "extractor":  {"local": _settings.extractor_model_local,  "remote": _settings.extractor_model_remote},
}

# ── Cache do health check ─────────────────────────────────────────────────────
_CACHE_TTL = _settings.remote_health_ttl_s
_last_ts: float = 0.0
_remote_ok: bool = False


def _check_remote() -> bool:
    """Health check contra a API remota, com resultado cacheado por _CACHE_TTL segundos."""
    global _last_ts, _remote_ok

    # Sem credencial não há provedor remoto — evita health check inútil.
    if not _REMOTE_API_KEY:
        return False

    now = time.monotonic()
    if now - _last_ts < _CACHE_TTL:
        return _remote_ok

    try:
        # timeout curto: no pior caso (remoto fora do ar) o custo é 3s por
        # janela de 60s de cache, não 5s.
        r = requests.get(f"{_REMOTE_URL}/health", timeout=_settings.remote_health_timeout_s)
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


# ── Chat LLM ─────────────────────────────────────────────────────────────────

def get_chat_llm(
    role: str = "chat",
    *,
    model: str | None = None,
    temperature: float = 0.0,
    num_ctx: int | None = None,
    timeout: float | None = None,
    **kwargs,
):
    """
    Cria um ChatOllama para o papel indicado, resolvendo modelo e endpoint pelo
    provedor ativo (remoto/local). Fonte ÚNICA de construção do cliente de chat —
    antes duplicada em nodes.py, lifestyle_monitor_agent.py e semantic_extractor.py.

    Mantém a dependência de `langchain_ollama` isolada na camada Infra: a Application
    passa a pedir um LLM por papel, sem conhecer a biblioteca cliente concreta.

    Args:
        role:        papel do modelo ('chat', 'extractor', ...).
        model:       nome explícito do modelo; sobrepõe a resolução por papel.
        temperature: 0.0 (default) para saída determinística de tool-calling.
        num_ctx:     janela de contexto; None usa o default do Ollama.
        timeout:     timeout (s) do httpx interno; None não injeta.
        **kwargs:    repassados ao ChatOllama (ex.: format="json").
    """
    from langchain_ollama import ChatOllama

    config = get_ollama_config()
    if timeout is not None:
        config.setdefault("client_kwargs", {})["timeout"] = timeout

    params: dict = {"model": model or get_model(role), "temperature": temperature}
    if num_ctx is not None:
        params["num_ctx"] = num_ctx

    return ChatOllama(**params, **config, **kwargs)


async def ainvoke_with_retry(llm, messages, *, config=None, attempts: int = 2, label: str = "LLM"):
    """
    Invoca o LLM de forma assíncrona com N tentativas.

    Falhas transitórias (rede, provedor remoto reiniciando) são comuns com Ollama;
    uma segunda tentativa resolve a maioria sem custo perceptível. A última falha
    propaga para o chamador. Centraliza o retry antes duplicado em nodes.py e
    lifestyle_monitor_agent.py.
    """
    for attempt in range(1, attempts + 1):
        try:
            if config is not None:
                return await llm.ainvoke(messages, config=config)
            return await llm.ainvoke(messages)
        except Exception as e:
            if attempt == attempts:
                raise
            _logger.warning(
                "🔁 [%s] Falha na chamada ao LLM (%s) — tentativa %d/%d...",
                label, e, attempt + 1, attempts,
            )


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


def list_models() -> list[dict]:
    """Retorna os modelos disponíveis no provedor ativo (remote ou local)."""
    config = get_ollama_config()
    base_url = config["base_url"]
    headers = config.get("client_kwargs", {}).get("headers", {})
    r = requests.get(f"{base_url}/api/tags", headers=headers, timeout=10)
    r.raise_for_status()
    return r.json().get("models", [])


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
