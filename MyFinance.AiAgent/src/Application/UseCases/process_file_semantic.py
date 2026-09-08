from __future__ import annotations

import logging
from typing import List

from src.Domain.interfaces import ISemanticExtractor
from src.Domain.models import ExtractedTransaction

_logger = logging.getLogger("myfinance.agent")


class ProcessFileSemanticUseCase:
    """
    Use-case de extração semântica de um arquivo de extrato.

    Recebe o caminho de um CSV ou PDF, lê o texto bruto e delega toda a
    interpretação ao SemanticExtractor: o LLM detecta data, valor, tipo e
    categoria a partir do texto.

    Este use-case é o *último recurso* da importação: a API só chega aqui quando
    nenhum parser determinístico reconheceu o arquivo. Ele também não resolve
    categoria — quem cruza a descrição com as categorias e o histórico do usuário
    é a API, que tem o banco. Aqui só sai extração crua.
    """

    def __init__(self, extractor: ISemanticExtractor) -> None:
        self._extractor = extractor

    # ── API pública ────────────────────────────────────────────────────────────

    def execute(self, file_path: str) -> List[ExtractedTransaction]:
        raw_text = self._read_file(file_path)
        extracted = self._extractor.extract_from_text(raw_text)

        _logger.info(
            "📊 [SEMANTIC UC] %d transações extraídas do arquivo '%s'.",
            len(extracted),
            file_path,
        )

        return extracted

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
            content = fh.read()

        lines = content.splitlines()
        non_blank = [l for l in lines if l.strip()]
        _logger.info(
            "📂 [SEMANTIC UC] Arquivo lido: %d chars | %d linhas totais | %d não-vazias",
            len(content),
            len(lines),
            len(non_blank),
        )
        return content
