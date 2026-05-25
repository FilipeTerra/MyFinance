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
from src.Infra.Llm.ollama_provider import get_ollama_config, get_model

_logger = logging.getLogger("myfinance.agent")


def _extract_user_id(jwt_token: str) -> str:
    """Extrai o 'sub' (userId) do JWT sem verificar a assinatura.
    A validação foi feita pelo .NET antes de chegar aqui."""
    payload = jwt.decode(jwt_token, options={"verify_signature": False})
    user_id = payload.get("sub")
    if not user_id:
        raise ValueError("Token inválido: claim 'sub' não encontrado.")
    return user_id

_SYSTEM_PROMPT = (
    "Você é o Claudio, o assistente financeiro pessoal do usuário. "
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
    "9. Para qualquer conselho, conceito, princípio ou pergunta sobre livros financeiros (reserva de emergência, regra 50/30/20, independência financeira, Pai Rico Pai Pobre, etc.), use SEMPRE a ferramenta consultar_teoria_financeira ANTES de responder. "
    "Quando a ferramenta retornar trechos dos livros, sua resposta DEVE explicar o que os livros ensinam, citando diretamente os conceitos retornados. NUNCA ignore o conteúdo dos trechos. "

    "Nunca invente dados ou cálculos — use apenas as ferramentas disponíveis e a literatura financeira. "
    "NUNCA fale sobre as ferramentas ou detalhes técnicos. Apenas diga como você pode ajudar e entregue a resposta de forma clara, objetiva e acionável."
)

# MemorySaver é singleton — histório de conversa persiste entre requests
_memory = MemorySaver()


async def invoke_chat(message: str, jwt_token: str) -> str:
    """
    Invoca o agente consultor mantendo histórico de conversa por usuário.
    O LLM é criado por request para refletir o provedor ativo (remoto/local).
    O MemorySaver garante que a memória da conversa persiste entre as chamadas.
    """
    user_id = _extract_user_id(jwt_token)

    model_name = get_model("chat")
    llm = ChatOllama(model=model_name, **get_ollama_config())

    _logger.info("📨 [CHAT] user=%s | modelo=%s | mensagem: %s", user_id[:8], model_name, message[:120])

    api_tools = make_api_tools(jwt_token)
    all_tools = [
        consultar_teoria_financeira,
        simular_juros_compostos,
        comparar_quitacao_vs_investimento,
    ] + api_tools

    _logger.info("⚙️  [AGENT] %d ferramentas registradas: %s", len(all_tools), [t.name for t in all_tools])

    agent = create_react_agent(
        llm,
        all_tools,
        prompt=SystemMessage(content=_SYSTEM_PROMPT),
        checkpointer=_memory,
    )

    config = {
        "configurable": {"thread_id": user_id},
        "callbacks": [AgentCallbackLogger(user_id, model_name=model_name)],
    }
    result = await agent.ainvoke(
        {"messages": [HumanMessage(content=message)]},
        config=config,
    )

    response = result["messages"][-1].content
    _logger.info("💬 [CHAT] Resposta gerada: %d chars", len(response))
    return response
