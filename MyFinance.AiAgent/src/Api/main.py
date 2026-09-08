import logging
import os
import tempfile
import time
import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from typing import List

from src.Infra.Config.settings import get_settings
from src.Infra.Llm.ollama_utils import ensure_model
from src.Infra.Llm.ollama_provider import list_models, get_model, is_remote, _MODELS
from src.Infra.Llm.category_suggester import CategorySuggester
from src.Infra.Llm.semantic_extractor import SemanticExtractor
from src.Infra.Data.financial_rag import FinancialKnowledgeBase
from src.Infra.Logging.agent_logger import setup_logging
from src.Application.UseCases.process_file_semantic import ProcessFileSemanticUseCase
import jwt
from src.Application.Agents.chat_consultant_agent import invoke_chat
from src.Application.Agents.proactive_analyzer_agent import invoke_proactive_analysis
from src.Application.Agents.lifestyle_monitor_agent import invoke_lifestyle_monitor

setup_logging()

_logger = logging.getLogger("myfinance.agent")
_settings = get_settings()
_knowledge_base = FinancialKnowledgeBase()

_BOOKS_DIR = _settings.books_dir


def _startup_sync() -> None:
    """
    Executado em thread separada: garante o modelo de chat/embeddings na subida.

    A ingestão de data/books/ NÃO roda mais automaticamente aqui: os livros são
    estáticos e raramente mudam, então reprocessar embeddings a cada subida do
    servidor era custo desnecessário. A ingestão agora é sob demanda, como uma
    migration — chame POST /api/ai/ingest ao adicionar ou atualizar um livro.
    """
    try:
        provider = "remoto" if is_remote() else "local"
        _logger.info(
            "🤖 [STARTUP] Modelo de chat ativo: %s (provedor: %s)",
            get_model("chat"),
            provider,
        )
    except Exception as e:
        _logger.warning("⚠️  [STARTUP] Não foi possível resolver o modelo de chat: %s", e)

    ensure_model(get_model("embedding"))


@asynccontextmanager
async def lifespan(_app: FastAPI):
    asyncio.create_task(asyncio.to_thread(_startup_sync))
    yield


app = FastAPI(title="MyFinance AI Agent", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=_settings.cors_allow_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class ChatRequest(BaseModel):
    jwt_token: str
    prompt: str
    context_payload: dict = Field(default_factory=dict)


class IngestRequest(BaseModel):
    directory: str = _BOOKS_DIR


class SuggestCategoriesRequest(BaseModel):
    descriptions: List[str]
    categories: List[str]


class ProactiveInsightRequest(BaseModel):
    jwt_token: str


@app.get("/health")
async def health():
    """
    Sinal de vida do agente, usado pela API para decidir em segundos se vale a
    pena tentar o caminho com IA. Não toca no Ollama de propósito: aqui a
    pergunta é "o processo está no ar?", não "o modelo responde?".
    """
    return {"status": "ok"}


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


@app.post("/api/ai/proactive/emergency-reserve")
async def proactive_emergency_reserve(request: ProactiveInsightRequest):
    try:
        payload = jwt.decode(request.jwt_token, options={"verify_signature": False})
        if time.time() > payload.get("exp", 0):
            return {"success": False, "error_type": "session_expired", "erro": "Token expirado."}

        return await invoke_proactive_analysis(request.jwt_token)
    except Exception:
        _logger.exception("❌ [PROACTIVE] Erro não tratado no endpoint /api/ai/proactive/emergency-reserve")
        return {
            "success": False,
            "erro": "Erro interno ao gerar o insight. Tente novamente em instantes.",
        }


@app.post("/api/ai/proactive/lifestyle-inflation")
async def proactive_lifestyle_inflation(request: ProactiveInsightRequest):
    try:
        payload = jwt.decode(request.jwt_token, options={"verify_signature": False})
        if time.time() > payload.get("exp", 0):
            return {"success": False, "error_type": "session_expired", "erro": "Token expirado."}

        return await invoke_lifestyle_monitor(request.jwt_token)
    except Exception:
        _logger.exception("❌ [LIFESTYLE] Erro não tratado no endpoint /api/ai/proactive/lifestyle-inflation")
        return {
            "success": False,
            "erro": "Erro interno ao gerar o insight. Tente novamente em instantes.",
        }


@app.post("/api/ai/extract-statement")
async def extract_statement(file: UploadFile = File(...)):
    """
    Extração semântica de um extrato que nenhum parser determinístico da API
    reconheceu. Devolve as transações cruas — sem conta, sem categoria resolvida:
    o cruzamento com as categorias e o histórico do usuário é feito na API, que
    é quem tem o banco.
    """
    # basename() neutraliza path traversal (ex: filename="../../etc/cron.d/x")
    safe_name = os.path.basename(file.filename or "upload.tmp")
    file_location = os.path.join(tempfile.gettempdir(), f"myfinance_{safe_name}")

    try:
        with open(file_location, "wb+") as file_object:
            file_object.write(await file.read())

        use_case = ProcessFileSemanticUseCase(SemanticExtractor())

        # A extração chama o LLM de forma síncrona e pode levar minutos —
        # roda em thread para não bloquear o event loop da API.
        extracted = await asyncio.to_thread(use_case.execute, file_location)

        return {
            "success": True,
            "transactions": [
                {
                    "date": t.data,
                    "description": t.descricao,
                    "valor": t.valor,
                    "tipo": t.tipo,
                    "categoria": t.categoria,
                }
                for t in extracted
            ],
        }

    except Exception:
        _logger.exception("❌ [FILE] Erro crítico ao processar extrato '%s'", safe_name)
        return {"success": False, "message": "Erro interno ao processar o arquivo. Tente novamente."}
    finally:
        # Não acumula uploads no diretório temporário
        try:
            os.remove(file_location)
        except OSError:
            pass


@app.post("/api/ai/suggest-categories")
async def suggest_categories(request: SuggestCategoriesRequest):
    """
    Sugere categoria para as descrições que a API não resolveu pelas regras
    aprendidas nem pelo histórico. Enriquecimento opcional: a API segue com as
    linhas em branco se este endpoint falhar.
    """
    try:
        suggester = CategorySuggester()
        suggestions = await asyncio.to_thread(
            suggester.suggest, request.descriptions, request.categories
        )
        return {"success": True, "suggestions": suggestions}
    except Exception:
        _logger.exception("❌ [SUGESTÃO] Erro ao sugerir categorias")
        return {"success": False, "suggestions": {}}
