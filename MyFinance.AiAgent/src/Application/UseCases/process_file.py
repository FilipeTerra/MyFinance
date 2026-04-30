# MyFinance.AiAgent/src/Application/UseCases/process_file.py
from src.Domain.interfaces import IFileExtractor, ILlmClassifier

class ProcessFileUseCase:
    def __init__(self, extractor: IFileExtractor, classifier: ILlmClassifier):
        self.extractor = extractor
        self.classifier = classifier

    def execute(self, file_path: str, account_id: str, existing_categories: dict) -> list:
        df = self.extractor.extract(file_path)
        category_names = list(existing_categories.keys())
        
        results = []
        for _, row in df.iterrows():
            ai_decision = self.classifier.classify_category(row['descricao'], category_names)
            
            # Mapeia de volta para IDs se não for novo para manter consistência
            category_id = existing_categories.get(ai_decision['categoryName'])
            
            results.append({
                "date": row['data'],
                "description": row['descricao'],
                "amount": float(row['valor']),
                "accountId": account_id,
                "categoryId": category_id, # Será nulo se isNew for True
                "suggestedCategoryName": ai_decision['categoryName'] if ai_decision['isNew'] else None,
                "isSuggestion": ai_decision['isNew']
            })
        return results