import logging
from typing import Any
from langchain_core.callbacks import BaseCallbackHandler
from langchain_core.outputs import LLMResult

_LOGGER_NAME = "myfinance.agent"


def setup_logging() -> None:
    """Configura o formato de log para toda a aplicação."""
    import os

    log_level = logging.DEBUG if os.getenv("MYFINANCE_DEBUG") else logging.INFO

    logging.basicConfig(
        level=log_level,
        format="%(asctime)s | %(levelname)-5s | %(message)s",
        datefmt="%H:%M:%S",
    )
    logging.getLogger(_LOGGER_NAME).setLevel(log_level)
    # Silencia logs verbosos de libs externas
    logging.getLogger("httpx").setLevel(logging.WARNING)
    logging.getLogger("httpcore").setLevel(logging.WARNING)
    logging.getLogger("uvicorn.access").setLevel(logging.WARNING)


class AgentCallbackLogger(BaseCallbackHandler):
    """
    Callback handler que intercepta os eventos do ciclo de vida do LangGraph
    e emite logs estruturados no terminal sem modificar nenhuma ferramenta.
    """

    def __init__(self, user_id: str, model_name: str = "desconhecido") -> None:
        super().__init__()
        self._logger = logging.getLogger(_LOGGER_NAME)
        self._user_id = user_id[:8]
        self._model_name = model_name

    # ── LLM / Chat Model ───────────────────────────────────────────────────

    def on_chat_model_start(
        self, serialized: dict, messages: list, **kwargs: Any
    ) -> None:
        self._logger.info("🤖 [LLM]  Pensando... (modelo: %s | user: %s)", self._model_name, self._user_id)

    def on_llm_end(self, response: LLMResult, **kwargs: Any) -> None:
        try:
            text = response.generations[0][0].text
            self._logger.info("🤖 [LLM]  Raciocínio concluído (%d chars)", len(text))
        except (IndexError, AttributeError):
            self._logger.info("🤖 [LLM]  Raciocínio concluído")

    def on_llm_error(self, error: BaseException, **kwargs: Any) -> None:
        self._logger.error("🤖 [LLM]  ❌ Erro: %s", error)

    # ── Tools ──────────────────────────────────────────────────────────────

    def on_tool_start(
        self, serialized: dict, input_str: str, **kwargs: Any
    ) -> None:
        tool_name = serialized.get("name", "desconhecida")
        preview = input_str[:150].replace("\n", " ")
        self._logger.info("🔧 [TOOL] Chamando '%s' | input: %s", tool_name, preview)

    def on_tool_end(self, output: Any, **kwargs: Any) -> None:
        preview = str(output)[:200].replace("\n", " ")
        self._logger.info("✅ [TOOL] Retorno: %s", preview)

    def on_tool_error(self, error: BaseException, **kwargs: Any) -> None:
        self._logger.error("❌ [TOOL] Erro: %s", error)
