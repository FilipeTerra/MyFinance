import pandas as pd
from src.Domain.interfaces import IFileExtractor

class CsvParser(IFileExtractor):
    def extract(self, file_path: str) -> pd.DataFrame:
        try:
            # skiprows=5 ignora o cabeçalho do banco (Conta, Período, Saldo, etc)
            # sep=';' define o separador correto
            # decimal=',' ensina o Pandas a ler '455,86' como número, não como texto
            df = pd.read_csv(
                file_path, 
                sep=';', 
                skiprows=5, 
                decimal=',',
                encoding='utf-8'
            )
            
            # Vamos printar para você ver no terminal o que o Pandas leu!
            print("--- DADOS LIDOS DO CSV ---")
            print(df.head())
            print("--------------------------")
            
            # Mapeamos as colunas do seu Banco para o padrão que a IA e o C# esperam
            df_padronizado = df.rename(columns={
                "Data Lançamento": "data",
                "Descrição": "descricao",
                "Valor": "valor"
            })
            
            # Retornamos apenas as colunas úteis
            return df_padronizado[["data", "descricao", "valor"]]
            
        except Exception as e:
            print(f"Erro ao processar o CSV: {e}")
            # Em caso de falha, retorna um DataFrame vazio para não quebrar a API
            return pd.DataFrame(columns=["data", "descricao", "valor"])