"""
test_settings.py — settings.py é a fonte central de config (Tier 0 do refactor)
e o ponto crítico de segurança: sem REMOTE_OLLAMA_API_KEY, o sistema precisa
degradar para o provedor local, nunca falhar ou vazar um default hardcoded.

Usamos Settings(_env_file=None) para isolar cada teste do .env real do projeto
(evita que o conteúdo do .env da máquina de quem roda o teste vaze pro resultado).
get_settings() é cacheado (@lru_cache) — não é usado aqui de propósito, para
poder variar env vars por teste sem interferência entre eles.
"""
from src.Infra.Config.settings import Settings, get_settings


def test_defaults_sem_env_nem_dotenv():
    s = Settings(_env_file=None)
    assert s.remote_ollama_api_key == ""
    assert s.remote_enabled is False
    assert s.local_ollama_url == "http://localhost:11434"
    assert s.books_dir == "data/books"
    assert s.debug is False


def test_remote_api_key_via_env_var_habilita_remote_enabled(monkeypatch):
    monkeypatch.setenv("REMOTE_OLLAMA_API_KEY", "chave-teste")
    s = Settings(_env_file=None)
    assert s.remote_ollama_api_key == "chave-teste"
    assert s.remote_enabled is True


def test_case_insensitive_env_vars(monkeypatch):
    monkeypatch.setenv("api_url", "http://exemplo.local/api")
    s = Settings(_env_file=None)
    assert s.api_url == "http://exemplo.local/api"


def test_debug_alias_myfinance_debug(monkeypatch):
    monkeypatch.setenv("MYFINANCE_DEBUG", "true")
    s = Settings(_env_file=None)
    assert s.debug is True


def test_model_for_resolve_por_papel_e_provedor():
    s = Settings(_env_file=None)
    assert s.model_for("chat", remote=False) == s.chat_model_local
    assert s.model_for("chat", remote=True) == s.chat_model_remote
    assert s.model_for("embedding", remote=False) == s.embedding_model_local


def test_get_settings_e_singleton_cacheado():
    assert get_settings() is get_settings()
