import logging
import jwt
from langchain_ollama import ChatOllama
from langchain_core.messages import HumanMessage, SystemMessage
from langgraph.prebuilt import create_react_agent
from langgraph.checkpoint.memory import MemorySaver
from src.Application.Agents.Tools.financial_tools import consultar_teoria_financeira
from src.Application.Agents.Tools.api_tools import make_api_tools
from src.Application.Agents.Tools.math_tools import (
    simular_juros_compostos,
    comparar_quitacao_vs_investimento,
)
from src.Infra.Logging.agent_logger import AgentCallbackLogger

_logger = logging.getLogger("myfinance.agent")


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
    "Você é o FinAl, o assistente financeiro pessoal do usuário. "
    "Sua comunicação deve ser EXTREMAMENTE objetiva, moderna e orientada à ação. "

    "Regras de formatação obrigatórias: "
    "1. Evite parágrafos longos e introduções demoradas. Vá direto ao ponto. "
    "2. Use emojis de forma estratégica para ilustrar tópicos (ex: 📊, 🎯, 🛡️). "
    "3. Use listas e negrito para destacar conceitos-chave e facilitar a leitura em tela. "
    "4. Sempre termine sugerindo 2 a 3 ações claras que o usuário pode tomar. "

    "Regras de uso de ferramentas: "
    "5. SEMPRE que o usuário pedir para projetar valores no futuro, calcular rendimentos ou simular investimentos, use OBRIGATORIAMENTE a ferramenta simular_juros_compostos. NUNCA calcule de cabeça. "
    "6. SEMPRE que o usuário perguntar se deve pagar uma dívida ou investir, use OBRIGATORIAMENTE a ferramenta comparar_quitacao_vs_investimento. NUNCA opine sem os números. "
    "7. Só crie metas financeiras com criar_meta_financeira quando o usuário pedir EXPLICITAMENTE. Nunca crie sem confirmação. "
    "8. Após realizar qualquer ação de criação, confirme ao usuário o que foi criado com os detalhes retornados pela ferramenta. "
    "9. Para qualquer conselho, estratégia ou princípio financeiro (reserva de emergência, regra 50/30/20, independência financeira, investimentos, orçamento), use SEMPRE a ferramenta consultar_teoria_financeira ANTES de responder. Baseie sua resposta nos trechos retornados. "

    "Nunca invente dados ou cálculos — use apenas as ferramentas disponíveis e a literatura financeira."
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

    _logger.info("📨 [CHAT] user=%s | mensagem: %s", user_id[:8], message[:120])

    api_tools = make_api_tools(jwt_token)
    all_tools = [
        consultar_teoria_financeira,
        simular_juros_compostos,
        comparar_quitacao_vs_investimento,
    ] + api_tools

    _logger.info("⚙️  [AGENT] %d ferramentas registradas: %s", len(all_tools), [t.name for t in all_tools])

    agent = create_react_agent(
        _llm,
        all_tools,
        prompt=SystemMessage(content=_SYSTEM_PROMPT),
        checkpointer=_memory,
    )

    config = {
        "configurable": {"thread_id": user_id},
        "callbacks": [AgentCallbackLogger(user_id, model_name=_MODEL_NAME)],
    }
    result = await agent.ainvoke(
        {"messages": [HumanMessage(content=message)]},
        config=config,
    )

    response = result["messages"][-1].content
    _logger.info("💬 [CHAT] Resposta gerada: %d chars", len(response))
    return response
