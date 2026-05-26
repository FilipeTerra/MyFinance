from __future__ import annotations

import logging
from datetime import datetime
from typing import List

from src.Domain.interfaces import ISemanticExtractor
from src.Domain.models import ExtractedTransaction
from src.Infra.Cache.knowledge_base import KnowledgeBase

_logger = logging.getLogger("myfinance.agent")

_DATE_FORMATS = [
    "%d/%m/%Y",   # 06/02/2025  (padrão brasileiro)
    "%m/%d/%Y",   # 02/06/2025  (padrão americano)
    "%Y-%m-%d",   # 2025-02-06  (ISO — já correto)
    "%d-%m-%Y",   # 06-02-2025
    "%d/%m/%y",   # 06/02/25
    "%Y/%m/%d",   # 2025/02/06
]


def _normalize_date(raw: str) -> str:
    """Converte qualquer formato de data reconhecido para YYYY-MM-DD."""
    raw = raw.strip()
    for fmt in _DATE_FORMATS:
        try:
            return datetime.strptime(raw, fmt).strftime("%Y-%m-%d")
        except ValueError:
            continue
    _logger.warning("[SEMANTIC UC] Data não reconhecida, mantendo original: '%s'", raw)
    return raw


def _to_title_case(name: str) -> str:
    """Normaliza nomes de categoria para Title Case. Ex: 'DROGARIA' → 'Drogaria'."""
    return name.strip().title() if name else name


class ProcessFileSemanticUseCase:
    """
    Use-case de ingestão de arquivo via extração semântica.

    Diferente do ProcessFileUseCase (que é determinístico + classificador),
    este delega toda a interpretação do documento ao SemanticExtractor:
    o LLM detecta data, valor, tipo e categoria diretamente do texto bruto.

    Suporta CSV e PDF — a leitura do arquivo é feita aqui antes de passar
    o texto puro para o extrator.
    """

    def __init__(self, extractor: ISemanticExtractor) -> None:
        self._extractor = extractor
        self._kb = KnowledgeBase()

    # ── API pública ────────────────────────────────────────────────────────────

    def execute(self, file_path: str, account_id: str, existing_categories: dict) -> list:
        """
        Processa o arquivo e retorna lista de transações no formato esperado
        pelo backend C# (mesmo schema do ProcessFileUseCase).
        """
        raw_text = self._read_file(file_path)
        extracted = self._extractor.extract_from_text(raw_text)

        _logger.info(
            "📊 [SEMANTIC UC] %d transações extraídas do arquivo '%s'.",
            len(extracted),
            file_path,
        )

        return self._map_to_output(extracted, account_id, existing_categories)

    # ── Internos ───────────────────────────────────────────────────────────────

    def _read_file(self, file_path: str) -> str:
        if file_path.lower().endswith(".pdf"):
            return self._read_pdf(file_path)
        return self._read_text(file_path)

    def _read_pdf(self, file_path: str) -> str:
        try:
            import pdfplumber

            pages: List[str] = []
            with pdfplumber.open(file_path) as pdf:
                for page in pdf.pages:
                    text = page.extract_text()
                    if text:
                        pages.append(text)
            return "\n".join(pages)
        except Exception as exc:
            _logger.error("❌ [SEMANTIC UC] Falha ao ler PDF '%s': %s", file_path, exc)
            raise

    def _read_text(self, file_path: str) -> str:
        with open(file_path, "r", encoding="utf-8", errors="replace") as fh:
            return fh.read()

    def _map_to_output(
        self,
        transactions: List[ExtractedTransaction],
        account_id: str,
        existing_categories: dict,
    ) -> list:
        results = []
        for t in transactions:
            amount = t.valor if t.tipo == "receita" else -t.valor

            # ── 1. Consulta a memória rápida (KB) ─────────────────────────────
            cached = self._kb.get_category(account_id, t.descricao)

            if cached:
                category_name = cached
                _logger.info("[RÁPIDO] '%s' → %s", t.descricao, category_name)

            elif t.categoria:
                # IA extraiu uma categoria do documento → só sugere, NÃO salva no KB
                # O KB só é atualizado quando o usuário confirmar via /api/ai/learn
                category_name = _to_title_case(t.categoria)
                _logger.info("[IA] '%s' → %s (sugestão pendente)", t.descricao, category_name)

            else:
                category_name = None

            # ── 2. Resolve o ID da categoria ──────────────────────────────────
            category_id = existing_categories.get(category_name) if category_name else None

            # is_suggestion=True quando não há ID correspondente nas categorias existentes
            is_suggestion = category_id is None
            suggested_name = category_name if is_suggestion and category_name else None

            results.append(
                {
                    "date": _normalize_date(t.data),
                    "description": t.descricao,
                    "amount": amount,
                    "accountId": account_id,
                    "categoryId": category_id,
                    "suggestedCategoryName": suggested_name,
                    "isSuggestion": is_suggestion,
                }
            )

        return results
