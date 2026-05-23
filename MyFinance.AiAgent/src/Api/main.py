import json
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from src.Infra.Llm.ollama_classifier import OllamaClassifier
from src.Infra.Parsers.csv_parser import CsvParser
from src.Infra.Data.financial_rag import FinancialKnowledgeBase
from src.Application.UseCases.process_file import ProcessFileUseCase
from src.Application.Agents.chat_consultant_agent import invoke_chat

app = FastAPI(title="MyFinance AI Agent")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


_knowledge_base = FinancialKnowledgeBase()


class ChatRequest(BaseModel):
    user_id: str
    jwt_token: str
    prompt: str


class IngestRequest(BaseModel):
    directory: str = "data/books"


@app.post("/api/ai/ingest")
async def ingest_documents(request: IngestRequest):
    try:
        total_chunks = _knowledge_base.ingest_documents(request.directory)
        return {"success": True, "message": f"{total_chunks} trechos indexados com sucesso."}
    except ValueError as e:
        return {"success": False, "message": str(e)}
    except Exception as e:
        return {"success": False, "message": f"Erro durante a ingestão: {str(e)}"}


@app.post("/api/ai/chat")
async def consultant_chat(request: ChatRequest):
    try:
        response = await invoke_chat(request.user_id, request.prompt, request.jwt_token)
        return {"success": True, "resposta": response}
    except Exception as e:
        return {"success": False, "erro": str(e)}


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
        classifier = OllamaClassifier()

        use_case = ProcessFileUseCase(extractor, classifier)

        processed_transactions = use_case.execute(file_location, accountId, existing_categories)

        return {"success": True, "data": processed_transactions}

    except Exception as e:
        print(f"Erro Crítico no Endpoint: {e}")
        return {"success": False, "message": f"Erro interno na IA: {str(e)}"}
