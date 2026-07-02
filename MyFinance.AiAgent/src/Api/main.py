import json
import logging
import os
import tempfile
import time
import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI, UploadFile, File, Form
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from typing import List

from src.Infra.Llm.ollama_utils import ensure_model
from src.Infra.Llm.ollama_provider import list_models, get_model, is_remote, _MODELS
from src.Infra.Cache.knowledge_base import KnowledgeBase
from src.Infra.Llm.semantic_extractor import SemanticExtractor
from src.Infra.Data.financial_rag import FinancialKnowledgeBase
from src.Infra.Logging.agent_logger import setup_logging
from src.Application.UseCases.process_file_semantic import ProcessFileSemanticUseCase
import jwt
from src.Application.Agents.chat_consultant_agent import invoke_chat

setup_logging()

_logger = logging.getLogger("myfinance.agent")
_knowledge_base = FinancialKnowledgeBase()

_BOOKS_DIR = "data/books"
_EMBEDDING_MODEL = "nomic-embed-text"


def _startup_sync() -> None:
    """Executado em thread separada: garante o modelo de embeddings e ingere documentos."""
    try:
        provider = "remoto" if is_remote() else "local"
        _logger.info(
            "🤖 [STARTUP] Modelo de chat ativo: %s (provedor: %s)",
            get_model("chat"),
            provider,
        )
    except Exception as e:
        _logger.warning("⚠️  [STARTUP] Não foi possível resolver o modelo de chat: %s", e)

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
    context_payload: dict = Field(default_factory=dict)


class IngestRequest(BaseModel):
    directory: str = "data/books"


class LearnRule(BaseModel):
    description: str
    category_name: str


class LearnRequest(BaseModel):
    account_id: str
    rules: List[LearnRule]


@app.get("/api/ai/models")
async def get_available_models():
    """Lista os modelos disponíveis no provedor Ollama ativo (remoto ou local)."""
    try:
        models = list_models()
        provider = "remote" if is_remote() else "local"
        return {"success": True, "provider": provider, "models": [m["name"] for m in models]}
    except Exception as e:
        return {"success": False, "message": str(e)}


@app.get("/api/ai/models/roles")
async def get_models_by_role():
    """Retorna o modelo configurado para cada papel (chat, classifier, embedding, extractor)."""
    provider = "remote" if is_remote() else "local"
    roles = {role: get_model(role) for role in _MODELS}
    return {"success": True, "provider": provider, "roles": roles}


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

        response = await invoke_chat(request.prompt, request.jwt_token, request.context_payload)
        return {"success": True, "resposta": response}
    except Exception:
        # Log completo no servidor; mensagem genérica ao cliente — detalhes
        # internos (URLs, stack, credenciais de conexão) não vazam na API.
        _logger.exception("❌ [CHAT] Erro não tratado no endpoint /api/ai/chat")
        return {
            "success": False,
            "erro": "Erro interno ao processar a mensagem. Tente novamente em instantes.",
        }


@app.post("/api/ai/learn")
async def learn_from_confirmed(request: LearnRequest):
    """
    Recebe as associações descrição→categoria confirmadas pelo usuário e
    persiste no knowledge_base. Chamado pelo frontend após o batch save.
    """
    kb = KnowledgeBase()
    saved = 0
    for rule in request.rules:
        if rule.description and rule.category_name:
            kb.add_rule(request.account_id, rule.description, rule.category_name)
            saved += 1
            _logger.info("📚 [KB] Aprendi: '%s' → '%s'", rule.description, rule.category_name)

    return {"success": True, "learned": saved}


async def _process_statement_impl(accountId: str, categoriesJson: str, file: UploadFile) -> dict:
    """
    Implementação única do Parser Semântico Universal: aceita qualquer CSV ou PDF
    de extrato bancário, sem mapeamento hardcoded de colunas. O LLM interpreta o
    documento e extrai data, descrição, valor, tipo e categoria do texto bruto.

    Compartilhada pelas rotas /process-file e /process-file-semantic (eram
    duplicadas linha a linha — agora há um único ponto de manutenção).
    """
    # basename() neutraliza path traversal (ex: filename="../../etc/cron.d/x")
    safe_name = os.path.basename(file.filename or "upload.tmp")
    file_location = os.path.join(tempfile.gettempdir(), f"myfinance_{safe_name}")

    try:
        with open(file_location, "wb+") as file_object:
            file_object.write(await file.read())

        existing_categories = json.loads(categoriesJson)

        extractor = SemanticExtractor()
        use_case = ProcessFileSemanticUseCase(extractor)

        # A extração chama o LLM de forma síncrona e pode levar minutos —
        # roda em thread para não bloquear o event loop da API.
        processed_transactions = await asyncio.to_thread(
            use_case.execute, file_location, accountId, existing_categories
        )

        return {"success": True, "data": processed_transactions}

    except Exception:
        _logger.exception("❌ [FILE] Erro crítico ao processar extrato '%s'", safe_name)
        return {"success": False, "message": "Erro interno ao processar o arquivo. Tente novamente."}
    finally:
        # Não acumula uploads no diretório temporário
        try:
            os.remove(file_location)
        except OSError:
            pass


@app.post("/api/ai/process-file")
async def process_statement(
    accountId: str = Form(...),
    categoriesJson: str = Form(...),
    file: UploadFile = File(...)
):
    return await _process_statement_impl(accountId, categoriesJson, file)


@app.post("/api/ai/process-file-semantic")
async def process_statement_semantic(
    accountId: str = Form(...),
    categoriesJson: str = Form(...),
    file: UploadFile = File(...)
):
    return await _process_statement_impl(accountId, categoriesJson, file)
