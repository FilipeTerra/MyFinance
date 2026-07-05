"""
errors.py — Erros de acesso ao backend .NET + decorator de tratamento.

Cada tool repetia o mesmo bloco (401 / offline / status inesperado / erro
genérico) no fim do corpo. Agora ApiSession.get_json/post_json levantam essas
exceções tipadas e @handle_api_errors as traduz na mensagem apropriada —
string (tools de chat) ou dict {"erro": ...} (tools que devolvem dados
estruturados).

IMPORTANTE: só erros de TRANSPORTE/autorização passam por aqui. Erros de
NEGÓCIO (ex: "usuário sem renda cadastrada") continuam sendo `return`
explícito dentro da ferramenta e trafegam intactos.
"""
import functools
import logging

import httpx

_logger = logging.getLogger("myfinance.agent")

ERR_OFFLINE = "Erro: A API financeira está offline ou inacessível."
ERR_SESSAO  = "Sessão expirada. O usuário precisa fazer login novamente."


class ApiError(Exception):
    """Falha de transporte/autorização ao acessar o backend .NET."""

class SessionExpired(ApiError):
    """JWT ausente, expirado ou inválido (HTTP 401)."""

class ApiOffline(ApiError):
    """Backend .NET inacessível (rede/porta)."""

class ApiStatus(ApiError):
    """Status HTTP inesperado (não-2xx e não-401)."""
    def __init__(self, status_code: int, detail: str = ""):
        self.status_code = status_code
        self.detail = detail
        super().__init__(f"status {status_code}")


def handle_api_errors(as_dict: bool = False):
    """Traduz as exceções de acesso ao .NET em retorno de erro (string ou dict)."""

    def _err(msg: str):
        return {"erro": msg} if as_dict else msg

    def deco(fn):
        @functools.wraps(fn)
        async def wrapper(*args, **kwargs):
            try:
                return await fn(*args, **kwargs)
            except SessionExpired:
                _logger.warning("🔑 [API] JWT rejeitado (401) em %s", fn.__name__)
                return _err(ERR_SESSAO)
            except (ApiOffline, httpx.RequestError) as e:
                _logger.warning("🔌 [API] Backend .NET offline em %s: %s", fn.__name__, e)
                return _err(ERR_OFFLINE)
            except ApiStatus as e:
                if e.status_code == 400 and e.detail:
                    _logger.warning("⚠️  [API] %s: dados inválidos (400)", fn.__name__)
                    return _err(f"Dados inválidos: {e.detail}")
                _logger.warning("⚠️  [API] %s respondeu status %d", fn.__name__, e.status_code)
                return _err(f"Erro ao acessar a API financeira (status {e.status_code}).")
            except Exception as e:
                _logger.error("❌ [API] Erro inesperado em %s: %s", fn.__name__, e)
                return _err(f"Erro inesperado ao acessar a API financeira: {e}")
        return wrapper
    return deco
