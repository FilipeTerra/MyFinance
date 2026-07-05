"""
settings.py — Configuração central da camada AiAgent.

Fonte única de verdade para variáveis de ambiente, endpoints, modelos e caminhos.
Substitui os `os.getenv`/constantes que antes viviam espalhados por ~9 arquivos.

Segurança:
  Segredos (ex.: REMOTE_OLLAMA_API_KEY) NÃO têm valor default real aqui — o
  default é vazio de propósito. A credencial vive APENAS em variável de ambiente
  ou no arquivo .env (que é git-ignored). Se o segredo estiver ausente, o sistema
  degrada com segurança (o provedor remoto é ignorado e cai no Ollama local),
  nunca vaza chave e nunca embute credencial no código versionado.

Uso:
    from src.Infra.Config.settings import get_settings
    settings = get_settings()
    settings.api_url
"""
from functools import lru_cache

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Configuração da aplicação, carregada de variáveis de ambiente e do .env."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,   # REMOTE_OLLAMA_URL == remote_ollama_url
        extra="ignore",         # ignora chaves desconhecidas no ambiente/.env
    )

    # ── Provedor Ollama: endpoints ─────────────────────────────────────────────
    remote_ollama_url: str = "http://ollama.futurelab.dcc.ufmg.br"
    # SEGREDO — sem default real. Ausência ⇒ provedor remoto desativado.
    remote_ollama_api_key: str = ""
    local_ollama_url: str = "http://localhost:11434"

    # ── Modelos por papel e por provedor ───────────────────────────────────────
    chat_model_local: str = "gemma4:latest"
    chat_model_remote: str = "llama3.1:8b"
    classifier_model_local: str = "llama3.2:3b"
    classifier_model_remote: str = "llama3.2:3b"
    embedding_model_local: str = "nomic-embed-text"
    embedding_model_remote: str = "nomic-embed-text:latest"
    extractor_model_local: str = "qwen2.5:7b"
    extractor_model_remote: str = "llama3.2:3b"

    # ── Health check do provedor remoto ─────────────────────────────────────────
    remote_health_ttl_s: float = 60.0
    remote_health_timeout_s: float = 3.0

    # ── Backend .NET ─────────────────────────────────────────────────────────────
    api_url: str = "http://localhost:5088/api"

    # ── Caminhos de dados (RAG e cache de categorização) ────────────────────────
    books_dir: str = "data/books"
    faiss_index_path: str = "data/faiss_index"
    knowledge_base_path: str = "knowledge_base.json"

    # ── CORS (frontend) ──────────────────────────────────────────────────────────
    cors_allow_origins: list[str] = ["http://localhost:5173"]

    # ── Logging ──────────────────────────────────────────────────────────────────
    debug: bool = Field(default=False, validation_alias="MYFINANCE_DEBUG")

    @property
    def remote_enabled(self) -> bool:
        """True quando há credencial para o provedor remoto."""
        return bool(self.remote_ollama_api_key)

    def model_for(self, role: str, remote: bool) -> str:
        """Retorna o modelo configurado para o papel no provedor indicado."""
        provider = "remote" if remote else "local"
        try:
            return getattr(self, f"{role}_model_{provider}")
        except AttributeError as exc:
            raise ValueError(f"Papel '{role}' ou provedor '{provider}' não configurado.") from exc


@lru_cache
def get_settings() -> Settings:
    """Instância única (cacheada) das configurações — carregada uma vez por processo."""
    return Settings()
