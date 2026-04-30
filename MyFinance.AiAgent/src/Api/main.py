# src/api/main.py
import json
from fastapi import FastAPI, UploadFile, File, Form
from typing import List

from src.Infra.Llm.ollama_classifier import OllamaClassifier
from src.Infra.Parsers.csv_parser import CsvParser
from src.Application.UseCases.process_file import ProcessFileUseCase

app = FastAPI(title="MyFinance AI Agent")

@app.post("/api/ai/process-file")
async def process_statement(
    accountId: str = Form(...),
    categoriesJson: str = Form(...), 
    file: UploadFile = File(...)
):
    file_location = f"/tmp/{file.filename}"
    with open(file_location, "wb+") as file_object:
        file_object.write(file.file.read())
        
    try:
        # Transforma o JSON de categorias em um Dicionário Python
        # Esperado: {"Alimentacao": "guid-1", "Transporte": "guid-2"}
        existing_categories = json.loads(categoriesJson)

        extractor = CsvParser() 
        classifier = OllamaClassifier(model_name="gemma4")
        
        use_case = ProcessFileUseCase(extractor, classifier)
        
        processed_transactions = use_case.execute(file_location, accountId, existing_categories)
        
        return {
            "success": True,
            "data": processed_transactions
        }
    
    except Exception as e:
        print(f"Erro Crítico no Endpoint: {e}")
        return {
            "success": False,
            "message": f"Erro interno na IA: {str(e)}"
        }