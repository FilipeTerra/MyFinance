import json
import logging
import os
import time
import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

from src.Infra.Llm.ollama_classifier import OllamaClassifier
from src.Infra.Llm.ollama_utils import ensure_model
from src.Infra.Parsers.csv_parser import CsvParser
from src.Infra.Data.financial_rag import FinancialKnowledgeBase
from src.Infra.Logging.agent_logger import setup_logging
from src.Application.UseCases.process_file import ProcessFileUseCase
import jwt
from src.Application.Agents.chat_consultant_agent import invoke_chat

setup_logging()

_logger = logging.getLogger("myfinance.agent")
_knowledge_base = FinancialKnowledgeBase()

_BOOKS_DIR = "data/books"
_EMBEDDING_MODEL = "nomic-embed-text"


def _startup_sync() -> None:
    """Executado em thread separada: garante o modelo de embeddings e ingere documentos."""
    ensure_model(_EMBEDDING_MODEL)

    if not os.path.isdir(_BOOKS_DIR):
        _logger.info("📚 [RAG]  Diretório '%s' não encontrado. RAG não será inicializado.", _BOOKS_DIR)
        return

    files = [f for f in os.listdir(_BOOKS_DIR) if f.lower().endswith((".pdf", ".txt"))]
    if not files:
        _logger.info(
            "📚 [RAG]  Nenhum .pdf/.txt encontrado em '%s'. "
            "Adicione documentos para ativar o RAG.",
            _BOOKS_DIR,
        )
        return

    _logger.info("📚 [RAG]  Ingerindo %d arquivo(s) de '%s'...", len(files), _BOOKS_DIR)
    try:
        total = _knowledge_base.ingest_documents(_BOOKS_DIR)
        _logger.info("✅ [RAG]  %d chunks indexados com sucesso.", total)
    except Exception as e:
        _logger.error("❌ [RAG]  Falha na ingestão automática: %s", e)


@asynccontextmanager
async def lifespan(_app: FastAPI):
    asyncio.create_task(asyncio.to_thread(_startup_sync))
    yield


app = FastAPI(title="MyFinance AI Agent", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class ChatRequest(BaseModel):
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
        payload = jwt.decode(request.jwt_token, options={"verify_signature": False})
        if time.time() > payload.get("exp", 0):
            return {"success": False, "error_type": "session_expired", "erro": "Token expirado."}

        response = await invoke_chat(request.prompt, request.jwt_token)
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
