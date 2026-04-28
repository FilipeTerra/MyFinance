# src/application/use_cases/process_file.py
from src.domain.interfaces import IFileExtractor, ILlmClassifier

class ProcessFileUseCase:
    # injeção de dependência
    def __init__(self, extractor: IFileExtractor, classifier: ILlmClassifier):
        self.extractor = extractor
        self.classifier = classifier

    def execute(self, file_path: str, account_id: str) -> list[dict]:
        # Extrai as linhas do arquivo (não importa se é PDF ou CSV)
        df_transactions = self.extractor.extract(file_path)
        
        processed_transactions = []
        
        for index, row in df_transactions.iterrows():
            description = row['descricao']
            amount = row['valor']
            date = row['data']
            
            # IA faz a classificacao
            category_id = self.classifier.classify_category(description)
            
            transaction_dto = {
                "description": description,
                "amount": float(amount),
                "type": 1 if float(amount) < 0 else 0, # 1=Expense, 0=Income
                "date": date,
                "accountId": account_id,
                "categoryId": category_id
            }
            processed_transactions.append(transaction_dto)
            
        return processed_transactions