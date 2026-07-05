# MyFinance.AiAgent/src/Domain/interfaces.py
from abc import ABC, abstractmethod
from typing import List


class ISemanticExtractor(ABC):
    @abstractmethod
    def extract_from_text(self, raw_text: str) -> List:
        """
        raw_text: texto bruto de extrato/fatura (CSV, PDF convertido em string).
        Retorna: List[ExtractedTransaction]
        """
        pass
