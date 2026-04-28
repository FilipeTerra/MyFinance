# src/domain/interfaces.py
from abc import ABC, abstractmethod
import pandas as pd

class IFileExtractor(ABC):
    """
    Qualquer classe que extraia dados de arquivos deve implementar este método,
    retornando um DataFrame padronizado, independente se é PDF, CSV ou TXT.
    """
    @abstractmethod
    def extract(self, file_path: str) -> pd.DataFrame:
        pass

class ILlmClassifier(ABC):
    """
    O classificador de IA deve receber a descrição de uma transação 
    e retornar o ID da categoria correspondente no banco de dados.
    """
    @abstractmethod
    def classify_category(self, description: str) -> str:
        pass