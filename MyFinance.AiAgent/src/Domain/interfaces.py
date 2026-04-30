# MyFinance.AiAgent/src/Domain/interfaces.py
from abc import ABC, abstractmethod
import pandas as pd

class IFileExtractor(ABC):
    @abstractmethod
    def extract(self, file_path: str) -> pd.DataFrame:
        pass

class ILlmClassifier(ABC):
    @abstractmethod
    def classify_category(self, description: str, existing_categories: list) -> dict:
        """
        existing_categories: lista de strings com os nomes das categorias atuais.
        Retorna: {"categoryName": str, "isNew": bool}
        """
        pass