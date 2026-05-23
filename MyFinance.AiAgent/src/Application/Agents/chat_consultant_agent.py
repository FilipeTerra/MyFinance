import jwt
from langchain_ollama import ChatOllama
from langchain_core.messages import HumanMessage, SystemMessage
from langgraph.prebuilt import create_react_agent
from langgraph.checkpoint.memory import MemorySaver
from src.Application.Agents.Tools.financial_tools import consultar_teoria_financeira
from src.Application.Agents.Tools.api_tools import make_api_tools


def _extract_user_id(jwt_token: str) -> str:
    """Extrai o 'sub' (userId) do JWT sem verificar a assinatura.
    A validação foi feita pelo .NET antes de chegar aqui."""
    payload = jwt.decode(jwt_token, options={"verify_signature": False})
    user_id = payload.get("sub")
    if not user_id:
        raise ValueError("Token inválido: claim 'sub' não encontrado.")
    return user_id

# LLM robusto reservado para raciocínio e conversação
_MODEL_NAME = "gemma4:latest"

_SYSTEM_PROMPT = (
    "Você é FinAl, o mentor financeiro pessoal do usuário. "
    "Seu papel é analisar comportamentos financeiros, detectar desperdícios de dinheiro, "
    "sugerir metas e oferecer conselhos baseados em literatura financeira consagrada. "
    "Seja direto, empático e prático. Nunca invente dados ou cálculos — use apenas "
    "as informações fornecidas pelo usuário ou por ferramentas disponíveis."
)

# Singletons: o LLM e a memória persistem entre requests
_memory = MemorySaver()
_llm = ChatOllama(model=_MODEL_NAME)


async def invoke_chat(message: str, jwt_token: str) -> str:
    """
    Invoca o agente consultor mantendo histórico de conversa por usuário.
    O user_id é extraído do JWT — o frontend só precisa enviar o token.
    O agente é recompilado por request para injetar as API tools com o JWT atual.
    O MemorySaver garante que a memória da conversa persiste entre as chamadas.
    """
    user_id = _extract_user_id(jwt_token)
    api_tools = make_api_tools(jwt_token)
    all_tools = [consultar_teoria_financeira] + api_tools

    agent = create_react_agent(
        _llm,
        all_tools,
        prompt=SystemMessage(content=_SYSTEM_PROMPT),
        checkpointer=_memory,
    )

    config = {"configurable": {"thread_id": user_id}}
    result = await agent.ainvoke(
        {"messages": [HumanMessage(content=message)]},
        config=config,
    )
    return result["messages"][-1].content
