from __future__ import annotations

import csv
import logging
import re
from datetime import datetime
from io import StringIO
from typing import List, Optional

from src.Domain.interfaces import ISemanticExtractor
from src.Domain.models import ExtractedTransaction
from src.Infra.Cache.knowledge_base import KnowledgeBase

# Colunas obrigatórias para o parser determinístico de CSV
_CSV_REQUIRED_COLS = {"Data", "Lançamento", "Valor"}

# Palavras-chave no campo "Tipo" do CSV → receita (crédito ao cartão)
_RECEITA_KEYWORDS = frozenset([
    "crédito", "credito", "estorno", "reembolso", "cashback",
    "pix recebido", "ted recebida", "doc recebido", "rendimento",
    "pagamento recebido",
])

# Categorias genéricas do banco que não ajudam o usuário
_CATEGORIA_IGNORAR = frozenset(["OUTROS", "outros", "Outros", ""])

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

        Para CSVs estruturados, usa parsing determinístico (sem LLM).
        Para PDFs ou CSVs não-reconhecidos, delega ao SemanticExtractor.
        """
        extracted: Optional[List[ExtractedTransaction]] = None

        if file_path.lower().endswith(".csv"):
            extracted = self._try_parse_csv(file_path)

        if extracted is None:
            raw_text = self._read_file(file_path)
            extracted = self._extractor.extract_from_text(raw_text)

        _logger.info(
            "📊 [SEMANTIC UC] %d transações extraídas do arquivo '%s'.",
            len(extracted),
            file_path,
        )

        return self._map_to_output(extracted, account_id, existing_categories)

    # ── Internos ───────────────────────────────────────────────────────────────

    def _try_parse_csv(self, file_path: str) -> Optional[List[ExtractedTransaction]]:
        """
        Tenta extrair transações diretamente do CSV sem usar o LLM.
        Retorna None se o arquivo não tiver o formato esperado, forçando
        o fallback para o SemanticExtractor.
        """
        try:
            with open(file_path, "r", encoding="utf-8-sig", errors="replace") as fh:
                content = fh.read()

            reader = csv.DictReader(StringIO(content))
            cols = set(reader.fieldnames or [])

            if not _CSV_REQUIRED_COLS.issubset(cols):
                _logger.info(
                    "📋 [CSV] Colunas %s não encontradas (tem: %s). Usando LLM.",
                    _CSV_REQUIRED_COLS - cols,
                    cols,
                )
                return None

            transactions: List[ExtractedTransaction] = []
            for row in reader:
                data = row.get("Data", "").strip()
                descricao = row.get("Lançamento", "").strip()
                valor_str = row.get("Valor", "").strip()
                categoria_raw = row.get("Categoria", "").strip()
                tipo_str = row.get("Tipo", "").strip().lower()

                if not data or not descricao or not valor_str:
                    continue

                # Determina se é crédito (valor negativo no extrato = dinheiro de volta)
                is_negative = valor_str.startswith("-")

                # Remove "R$", "-", pontos de milhar; troca vírgula decimal por ponto
                valor_clean = re.sub(r"[^\d,]", "", valor_str).replace(",", ".")
                valor = float(valor_clean) if valor_clean else 0.0

                # Tipo: sinal tem prioridade; depois palavras-chave do campo "Tipo"
                if is_negative or any(kw in tipo_str for kw in _RECEITA_KEYWORDS):
                    tipo = "receita"
                else:
                    tipo = "despesa"

                # Categoria: descarta genéricas como "OUTROS"
                categoria = (
                    None if categoria_raw in _CATEGORIA_IGNORAR else categoria_raw
                )

                transactions.append(
                    ExtractedTransaction(
                        data=data,
                        descricao=descricao,
                        valor=valor,
                        tipo=tipo,
                        categoria=categoria,
                    )
                )

            _logger.info(
                "📋 [CSV] Parse determinístico: %d transações extraídas (sem LLM).",
                len(transactions),
            )
            return transactions if transactions else None

        except Exception as exc:
            _logger.warning(
                "⚠️  [CSV] Falha no parse direto (%s). Usando LLM como fallback.", exc
            )
            return None

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
            content = fh.read()

        lines = content.splitlines()
        non_blank = [l for l in lines if l.strip()]
        _logger.info(
            "📂 [SEMANTIC UC] Arquivo lido: %d chars | %d linhas totais | %d não-vazias",
            len(content),
            len(lines),
            len(non_blank),
        )
        _logger.info("📂 [SEMANTIC UC] Primeiras 5 linhas do arquivo:")
        for i, ln in enumerate(lines[:5], 1):
            _logger.info("   [%02d] %s", i, ln[:150])
        if len(lines) > 5:
            _logger.info("📂 [SEMANTIC UC] Últimas 3 linhas do arquivo:")
            for i, ln in enumerate(lines[-3:], len(lines) - 2):
                _logger.info("   [%02d] %s", i, ln[:150])
        return content

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
