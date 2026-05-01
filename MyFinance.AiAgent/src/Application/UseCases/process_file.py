import pandas as pd
from datetime import datetime
from src.Domain.interfaces import IFileExtractor, ILlmClassifier

class ProcessFileUseCase:
    def __init__(self, extractor: IFileExtractor, classifier: ILlmClassifier):
        self.extractor = extractor
        self.classifier = classifier

    def execute(self, file_path: str, account_id: str, existing_categories: dict) -> list:
        df = self.extractor.extract(file_path)
        category_names = list(existing_categories.keys())
        
        results = []
        for index, row in df.iterrows():
            try:
                data_str = str(row['data']).strip()
                try:
                    data_obj = datetime.strptime(data_str, "%d/%m/%Y")
                    data_iso = data_obj.strftime("%Y-%m-%d")
                except ValueError:
                    data_iso = data_str # Se já estiver em outro formato, mantém

                valor_str = str(row['valor']).replace(".", "").replace(",", ".")
                valor_float = float(valor_str)

                ai_decision = self.classifier.classify_category(str(row['descricao']), category_names)
                
                category_id = existing_categories.get(ai_decision['categoryName'])
                
                results.append({
                    "date": data_iso,
                    "description": str(row['descricao']),
                    "amount": valor_float,
                    "accountId": account_id,
                    "categoryId": category_id,
                    "suggestedCategoryName": ai_decision['categoryName'] if ai_decision['isNew'] else None,
                    "isSuggestion": ai_decision['isNew']
                })
            except Exception as e:
                print(f"Erro ao processar a linha {index}: {e}")
                continue
                
        return results