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

    @abstractmethod
    def classify_batch(self, descriptions: list, existing_categories: list) -> list:
        """
        existing_categories: lista de strings com os nomes das categorias atuais.
        descriptions: lista de strings com as descrições das transações.
        Retorna: lista de objetos com as chaves "description", "categoryName" e "isNew".
        """
        pass