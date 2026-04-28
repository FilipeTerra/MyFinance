# src/api/main.py
from fastapi import FastAPI, UploadFile, File, Form
from typing import List

app = FastAPI(title="MyFinance AI Agent")

@app.post("/api/ai/process-statement")
async def process_statement(
    accountId: str = Form(...),
    file: UploadFile = File(...)
):
    file_location = f"/tmp/{file.filename}"
    with open(file_location, "wb+") as file_object:
        file_object.write(file.file.read())
        
    # TODO (Próximo passo): Instanciar as implementações reais de PDFParser e OllamaClient
    # pdf_parser = PdfParser()
    # ollama_client = OllamaClient()
    # use_case = ProcessStatementUseCase(pdf_parser, ollama_client)
    # result = use_case.execute(file_location, accountId)
    
    # Retorno mockado para você testar a comunicação C# -> Python agora
    return {
        "success": True,
        "data": [
            {"description": "Teste Extração", "amount": 100.0, "categoryId": "id-mockado"}
        ]
    }