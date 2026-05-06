# src/Api/main.py
import json
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List

from src.Infra.Llm.ollama_classifier import OllamaClassifier
from src.Infra.Parsers.csv_parser import CsvParser
from src.Application.UseCases.process_file import ProcessFileUseCase

from langchain_community.llms import Ollama

app = FastAPI(title="MyFinance AI Agent")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class ChatRequest(BaseModel):
    prompt: str

@app.post("/api/ai/chat")
async def ollama_chat(request: ChatRequest):
    try:
        llm = Ollama(model="gemma4")
        
        resposta = llm.invoke(request.prompt)
        
        return {
            "success": True,
            "resposta": resposta
        }
    except Exception as e:
        print(f"Erro no chat do Ollama: {e}")
        return {
            "success": False,
            "erro": str(e)
        }

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