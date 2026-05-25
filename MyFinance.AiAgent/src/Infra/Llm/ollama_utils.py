import logging
import requests

_logger = logging.getLogger("myfinance.agent")


def ensure_model(model_name: str) -> None:
    """
    Garante que o modelo está disponível no provedor ativo.
    - Remoto: apenas verifica presença na lista de tags (não faz pull).
    - Local: faz pull automático se o modelo não for encontrado.
    """
    from src.Infra.Llm.ollama_provider import get_ollama_config, is_remote

    config = get_ollama_config()
    base_url = config["base_url"]
    headers = config.get("client_kwargs", {}).get("headers", {})

    try:
        resp = requests.get(f"{base_url}/api/tags", headers=headers, timeout=10)
        if resp.status_code == 200:
            available = [m["name"] for m in resp.json().get("models", [])]
            if any(model_name in m for m in available):
                _logger.info("✅ [OLLAMA] Modelo '%s' disponível (%s).", model_name,
                             "remoto" if is_remote() else "local")
                return
    except requests.exceptions.ConnectionError:
        _logger.error("❌ [OLLAMA] Servidor inacessível em %s.", base_url)
        return
    except Exception as e:
        _logger.error("❌ [OLLAMA] Erro ao verificar tags: %s", e)
        return

    if is_remote():
        _logger.warning("⚠️  [OLLAMA] Modelo '%s' não encontrado na API remota.", model_name)
        return

    # Provedor local — faz pull
    _logger.info("⬇️  [OLLAMA] Baixando modelo '%s' localmente (pode demorar)...", model_name)
    try:
        pull = requests.post(
            f"{base_url}/api/pull",
            json={"model": model_name, "stream": False},
            timeout=600,
        )
        if pull.status_code == 200:
            _logger.info("✅ [OLLAMA] Modelo '%s' baixado com sucesso.", model_name)
        else:
            _logger.error("❌ [OLLAMA] Falha ao baixar '%s': %s", model_name, pull.text[:200])
    except Exception as e:
        _logger.error("❌ [OLLAMA] Erro ao baixar modelo '%s': %s", model_name, e)
