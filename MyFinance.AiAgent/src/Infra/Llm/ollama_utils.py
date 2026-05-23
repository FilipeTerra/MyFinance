import logging
import os
import requests

_logger = logging.getLogger("myfinance.agent")
_OLLAMA_BASE_URL = os.getenv("OLLAMA_URL", "http://localhost:11434")


def ensure_model(model_name: str) -> None:
    """Verifica se o modelo está disponível no Ollama e faz pull se necessário."""
    try:
        resp = requests.get(f"{_OLLAMA_BASE_URL}/api/tags", timeout=10)
        if resp.status_code == 200:
            available = [m["name"] for m in resp.json().get("models", [])]
            if any(model_name in m for m in available):
                _logger.info("✅ [OLLAMA] Modelo '%s' já disponível.", model_name)
                return

        _logger.info("⬇️  [OLLAMA] Baixando modelo '%s' (pode demorar alguns minutos)...", model_name)
        pull = requests.post(
            f"{_OLLAMA_BASE_URL}/api/pull",
            json={"model": model_name, "stream": False},
            timeout=600,
        )
        if pull.status_code == 200:
            _logger.info("✅ [OLLAMA] Modelo '%s' baixado com sucesso.", model_name)
        else:
            _logger.error("❌ [OLLAMA] Falha ao baixar '%s': %s", model_name, pull.text[:200])
    except requests.exceptions.ConnectionError:
        _logger.error("❌ [OLLAMA] Servidor Ollama inacessível em %s.", _OLLAMA_BASE_URL)
    except Exception as e:
        _logger.error("❌ [OLLAMA] Erro inesperado ao verificar modelo '%s': %s", model_name, e)
