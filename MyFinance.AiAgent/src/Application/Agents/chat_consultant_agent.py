"""
chat_consultant_agent.py — Ponto de Entrada do Agente Consultor Financeiro

Refatorado no Passo 4: create_react_agent substituído pelo StateGraph compilado.

Contrato público:
  invoke_chat(message, jwt_token, context_payload?) → str

Fluxo de execução:
  1. Extrai user_id do JWT (sem verificar assinatura — .NET já validou).
  2. Compila o grafo para esta requisição (JWT baked-in nos tools de API).
  3. Monta o estado inicial com a mensagem do usuário.
  4. Invoca o grafo com o checkpointer de memória (histórico por user_id).
  5. Retorna o conteúdo da última mensagem do estado final.

Extensão futura — context_payload:
  O parâmetro context_payload recebe o pacote de contexto pré-carregado pelo
  .NET (saldos, metas, resumo mensal). Quando o endpoint /api/ai/chat passar
  a enviar este payload, basta propagá-lo para invoke_chat sem outras mudanças.
  Atualizar ChatRequest em main.py e adicionar:
    response = await invoke_chat(request.prompt, request.jwt_token, request.context_payload)
"""

import logging

import jwt
from langchain_core.messages import AIMessage, BaseMessage, HumanMessage, ToolMessage

from src.Application.Agents.graph import create_agent_graph
from src.Infra.Llm.ollama_provider import get_model
from src.Infra.Logging.agent_logger import AgentCallbackLogger

_logger = logging.getLogger("myfinance.agent")

# Ferramentas que retornam dados estruturados com valores monetários.
# Se o LLM (especialmente modelos pequenos) ignorar o output dessas ferramentas,
# o dado é injetado diretamente na resposta final antes de enviar ao frontend.
_DATA_TOOLS = frozenset({
    "calcular_resumo_financeiro",
    "analisar_gastos_por_categoria",
    "simular_investimento",
    "consultar_metas_financeiras",
    "consultar_saldos_contas",
    "buscar_taxa_selic",
    "relatorio_mensal_por_categoria",
})


# ===========================================================================
# Helpers privados
# ===========================================================================

def _extract_user_id(jwt_token: str) -> str:
    """
    Extrai o claim 'sub' (userId) do JWT sem verificar a assinatura.
    A validação criptográfica é responsabilidade do middleware .NET;
    aqui apenas lemos o payload para identificar o thread de memória.

    Raises:
        ValueError: se o claim 'sub' estiver ausente no token.
    """
    payload = jwt.decode(jwt_token, options={"verify_signature": False})
    user_id = payload.get("sub")
    if not user_id:
        raise ValueError("Token inválido: claim 'sub' não encontrado.")
    return user_id


def _extract_final_response(messages: list[BaseMessage]) -> str:
    """
    Extrai a resposta final do grafo, garantindo que dados retornados por
    ferramentas de dados cheguem ao usuário mesmo quando o LLM os ignora.

    Fluxo:
      1. Pega o conteúdo da última mensagem (resposta do LLM).
      2. Identifica mensagens do turno atual (após o último HumanMessage).
      3. Coleta outputs de ferramentas de dados (_DATA_TOOLS) desse turno.
      4. Se o LLM não incluiu valores monetários (R$) mas a ferramenta os
         retornou, injeta o output da ferramenta antes da resposta do LLM.
         Isso é necessário especialmente em modelos pequenos (≤7b) que tendem
         a parafrasear ou ignorar dados estruturados retornados pelas tools.
    """
    if not messages:
        _logger.error("❌ [CHAT] Estado final sem mensagens — retornando fallback de emergência")
        return "Ocorreu um erro interno. Por favor, tente novamente."

    last: BaseMessage = messages[-1]
    content = last.content

    if not content:
        _logger.warning("⚠️  [CHAT] Última mensagem com content vazio — tipo: %s", type(last).__name__)
        return "Não consegui gerar uma resposta. Tente reformular sua pergunta."

    ai_response = str(content)

    # Localiza o início do turno atual (mensagens após o último HumanMessage)
    last_human_idx = next(
        (i for i in range(len(messages) - 1, -1, -1) if isinstance(messages[i], HumanMessage)),
        -1,
    )
    current_turn = messages[last_human_idx + 1 :]

    # Coleta outputs de ferramentas de dados do turno atual
    tool_outputs = [
        str(msg.content)
        for msg in current_turn
        if isinstance(msg, ToolMessage)
        and getattr(msg, "name", None) in _DATA_TOOLS
        and msg.content
    ]

    # Injeta os dados da ferramenta se o LLM não os incluiu na resposta
    tool_data = "\n\n".join(tool_outputs)
    if tool_data and "R$" in tool_data and "R$" not in ai_response:
        _logger.info(
            "🔧 [CHAT] LLM não incluiu dados da ferramenta — injetando na resposta final"
        )
        ai_response = tool_data + "\n\n---\n\n" + ai_response

    return ai_response


# ===========================================================================
# Função pública
# ===========================================================================

async def invoke_chat(
    message: str,
    jwt_token: str,
    context_payload: dict,
) -> str:
    """
    Invoca o Agente Consultor Financeiro via StateGraph compilado.

    Mantém histórico de conversa por usuário através do MemorySaver configurado
    no grafo (thread_id = user_id). Cada chamada appenda a mensagem do usuário
    ao histórico existente — a conversa é contínua e multi-turno por design.

    Args:
        message:         Texto enviado pelo usuário via frontend.
        jwt_token:       JWT Bearer extraído do header Authorization pelo .NET.
                         Usado para: (1) identificar o thread de memória;
                                     (2) autenticar chamadas às tools de API.
        context_payload: Pacote de contexto financeiro pré-carregado pelo .NET
                         (saldos, transações recentes, metas, resumo mensal).
                         Passado via config["configurable"]["context_payload"]
                         para o nó inject_context. Opcional — se ausente, o
                         agente obtém os dados chamando as ferramentas de API.

    Returns:
        Resposta textual gerada pelo agente, pronta para exibição no frontend.

    Raises:
        ValueError: se o JWT não contiver o claim 'sub'.
        Exception:  erros de LLM ou rede são propagados para o endpoint FastAPI,
                    que os captura e retorna {"success": false, "erro": ...}.
    """
    user_id = _extract_user_id(jwt_token)
    model_name = get_model("chat")

    _logger.info(
        "📨 [CHAT] Iniciando | user=%s | modelo=%s | msg=%d chars",
        user_id[:8],
        model_name,
        len(message),
    )

    # ── Compilação do grafo para esta requisição ─────────────────────────────
    # Cria um novo grafo com as tools de API autenticadas com o JWT do usuário.
    # O histórico de conversa é preservado pelo _memory (singleton no graph.py)
    # usando thread_id como chave de isolamento entre usuários.
    graph = create_agent_graph(jwt_token)

    # ── Estado inicial ───────────────────────────────────────────────────────
    # iterations=0: o contador começa do zero a cada nova invocação da conversa.
    # context_data={}: o nó inject_context populará este campo com context_payload.
    # messages=[HumanMessage(...)]: a nova mensagem do usuário é o único input;
    #   o MemorySaver já possui o histórico anterior e o grafo fará o merge.
    initial_state: dict = {
        "messages": [HumanMessage(content=message)],
        "context_data": {},
        "iterations": 0,
    }

    # ── Configuração da invocação ────────────────────────────────────────────
    # thread_id=user_id: chave de isolamento para o MemorySaver.
    # context_payload: lido pelo nó inject_context para hidratar context_data.
    # callbacks: AgentCallbackLogger registra cada step do LLM e das ferramentas.
    config: dict = {
        "configurable": {
            "thread_id": user_id,
            "context_payload": context_payload,
        },
        "callbacks": [AgentCallbackLogger(user_id, model_name=model_name)],
    }

    # ── Invocação do grafo ───────────────────────────────────────────────────
    result: dict = await graph.ainvoke(initial_state, config=config)

    response = _extract_final_response(result.get("messages", []))

    _logger.info(
        "💬 [CHAT] Concluído | user=%s | iterações=%d | resposta=%d chars",
        user_id[:8],
        result.get("iterations", 0),
        len(response),
    )

    return response
