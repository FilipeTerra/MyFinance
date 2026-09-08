from __future__ import annotations

import json
import logging
import re
from typing import Dict, List

from langchain_core.messages import HumanMessage, SystemMessage

from src.Infra.Llm.ollama_provider import get_chat_llm

_logger = logging.getLogger("myfinance.agent")

_SYSTEM_PROMPT = """Você classifica lançamentos financeiros brasileiros em categorias.

Você recebe uma lista de descrições de lançamentos e a lista de categorias que o \
usuário já usa. Para cada descrição, escolha a categoria mais adequada ENTRE AS \
CATEGORIAS INFORMADAS.

REGRAS:
- Use exatamente o nome da categoria como foi informado, sem inventar variações.
- Se nenhuma categoria informada servir, omita aquela descrição da resposta. \
É melhor não sugerir nada do que sugerir errado — o usuário classifica manualmente.
- Não invente descrições que não estavam na lista.

Retorne SOMENTE um JSON válido, sem texto extra, no formato:
{"sugestoes": {"DESCRIÇÃO EXATA COMO RECEBIDA": "Nome da Categoria"}}"""


class CategorySuggester:
    """
    Sugere categoria para as descrições que a API não conseguiu resolver de forma
    determinística. É um enriquecimento opcional da importação: a API trata falha
    e ausência de resposta como "sem sugestão", nunca como erro.
    """

    def __init__(self, batch_size: int = 25) -> None:
        self.batch_size = batch_size
        self._llm = get_chat_llm("classifier", temperature=0, format="json")

    def suggest(self, descriptions: List[str], categories: List[str]) -> Dict[str, str]:
        if not descriptions or not categories:
            return {}

        suggestions: Dict[str, str] = {}
        allowed = {c.strip().lower(): c.strip() for c in categories if c and c.strip()}

        for start in range(0, len(descriptions), self.batch_size):
            batch = descriptions[start : start + self.batch_size]
            try:
                suggestions.update(self._suggest_batch(batch, list(allowed.values()), allowed))
            except Exception as exc:
                _logger.warning("⚠️  [SUGESTÃO] Falha no lote iniciado em %d: %s", start, exc)

        _logger.info(
            "🏷️  [SUGESTÃO] %d de %d descrições receberam sugestão.",
            len(suggestions),
            len(descriptions),
        )
        return suggestions

    # ── Internos ───────────────────────────────────────────────────────────────

    def _suggest_batch(
        self, descriptions: List[str], categories: List[str], allowed: Dict[str, str]
    ) -> Dict[str, str]:
        payload = {"lancamentos": descriptions, "categorias": categories}

        messages = [
            SystemMessage(content=_SYSTEM_PROMPT),
            HumanMessage(content=json.dumps(payload, ensure_ascii=False)),
        ]

        response = self._llm.invoke(messages)
        raw = getattr(response, "content", "") or ""

        match = re.search(r"\{.*\}", raw, re.DOTALL)
        if not match:
            return {}

        data = json.loads(match.group(0))
        proposed = data.get("sugestoes") or {}

        # O modelo pode devolver categoria fora da lista ou descrição que não foi
        # enviada. Nesse caso a sugestão é descartada: categoria inventada viraria
        # categoria nova no banco sem o usuário ter pedido.
        valid_descriptions = set(descriptions)
        result: Dict[str, str] = {}

        for description, category in proposed.items():
            if description not in valid_descriptions or not isinstance(category, str):
                continue

            canonical = allowed.get(category.strip().lower())
            if canonical:
                result[description] = canonical

        return result
