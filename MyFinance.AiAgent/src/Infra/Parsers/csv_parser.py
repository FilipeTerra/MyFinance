# src/Infra/Parsers/csv_parser.py
import pandas as pd
from src.Domain.interfaces import IFileExtractor

class CsvParser(IFileExtractor):
    def extract(self, file_path: str) -> pd.DataFrame:
        df = pd.read_csv(file_path)
        
        df_padronizado = df.rename(columns={
            "Data": "data",
            "Descricao": "descricao",
            "Valor": "valor"
        })
        
        return df_padronizado