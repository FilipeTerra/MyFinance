"""
graph.py — Fábrica e Compilação do StateGraph LangGraph

Passo 4 (final) da refatoração para Workflow Determinístico com ciclo ReAct.

Este módulo é o ponto de montagem: conecta todos os artefatos dos passos
anteriores (AgentState, ferramentas, nós) em um grafo executável.

Topologia do grafo:
                          ┌─────────────────────────────────────────┐
                          │                                         │
    START → inject_context → agent → fix_agent_output ─tool_calls──→ tools
                                              │
                                              ├── sem tool_calls ──→ END
                                              │
                                              └── guardrail ──→ fallback ──→ END
                                                  (iterations >= MAX_ITERATIONS)

    fix_agent_output (Camada 1 anti-leak):
      Converte tool_calls emitidos como texto pelo LLM em AIMessage estruturada
      antes que o roteador tome sua decisão. Modelos pequenos (≤7b) frequentemente
      emitem JSON cru no .content em vez de popular o .tool_calls.

Responsabilidades deste módulo:
  - fallback_node        : barreira de segurança contra exaustão cognitiva.
  - _route               : roteador local com saída "fallback" explícita.
  - create_agent_graph   : factory pública que monta e compila o grafo por request.
"""

import logging
from typing import Literal

from langchain_core.messages import AIMessage
from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import END, START, StateGraph

from src.Application.Agents.nodes import inject_context, make_nodes
from src.Application.Agents.state import AgentState, MAX_ITERATIONS
from src.Application.Agents.tool_call_parser import fix_agent_output

_logger = logging.getLogger("myfinance.agent")

# ---------------------------------------------------------------------------
# Singleton do checkpointer de memória
#
# MemorySaver mantém o histórico de conversas em RAM, isolado por thread_id
# (= user_id extraído do JWT). O grafo é recriado por request (JWT novo),
# mas o checkpointer é compartilhado — a conversa persiste entre requests
# do mesmo usuário sem vazar estado entre usuários diferentes.
# ---------------------------------------------------------------------------
_memory = MemorySaver()

# ---------------------------------------------------------------------------
# Mensagem de fallback exibida ao usuário quando o guardrail é ativado
# ---------------------------------------------------------------------------
_FALLBACK_CONTENT = (
    "🛡️ Precisei interromper minha análise.\n\n"
    "Atingi o limite de operações para esta pergunta — isso geralmente acontece "
    "quando a consulta exige muitas etapas simultâneas ou quando alguma ferramenta "
    "encontrou dificuldades repetidas.\n\n"
    "Para me ajudar a responder melhor:\n"
    "• **Divida a pergunta** em partes menores e independentes\n"
    "• **Informe os valores diretamente** (ex: taxa, prazo, valor)\n"
    "• **Tente novamente** — instabilidades de rede podem causar isso\n\n"
    "Estou pronto para a próxima pergunta! 💪"
)


# ===========================================================================
# Nó de fallback — barreira de segurança vital do grafo
# ===========================================================================

def fallback_node(state: AgentState) -> dict:
    """
    Barreira de segurança (guardrail) contra exaustão cognitiva do agente.

    ARQUITETURA — por que este nó existe:
      O ciclo ReAct (Reasoning + Acting) pode entrar em loop quando:
        • Uma ferramenta falha repetidamente (ex: API .NET offline).
        • O LLM gera tool_calls mal formados que são re-tentados indefinidamente.
        • O agente raciocina em círculo sem convergir para uma conclusão.

      Sem este nó, quando o roteador detecta iterations >= MAX_ITERATIONS,
      o grafo encerraria com a última AIMessage contendo tool_calls não resolvidos
      no histórico — um estado inválido que causaria erros no consumidor.

      Este nó SUBSTITUI a mensagem suja (com tool_calls) por uma AIMessage limpa
      e amigável, garantindo que:
        1. O histórico de mensagens termina sempre em um estado válido.
        2. O usuário recebe uma resposta controlada, não silêncio ou erro.
        3. O fluxo de falhas críticas é isolado sem crashar o processo.

    Mecanismo de substituição:
      Reutiliza o `id` da última mensagem (a suja com tool_calls).
      O reducer `add_messages` do LangGraph detecta IDs duplicados e faz
      UPDATE in-place em vez de APPEND — o histórico fica limpo.
      Se a mensagem não tiver `id` (None), uma nova mensagem é simplesmente
      appendada como último item, o que também produz o resultado correto.

    Args:
        state: AgentState com a última mensagem contendo tool_calls pendentes.

    Returns:
        dict parcial {"messages": [AIMessage]} com mensagem limpa para merge.
    """
    last_msg = state["messages"][-1]
    last_id: str | None = getattr(last_msg, "id", None)

    # Substitui in-place pelo mesmo id (add_messages faz UPDATE, não APPEND)
    clean_msg = AIMessage(content=_FALLBACK_CONTENT, id=last_id)

    _logger.warning(
        "🛡️  [FALLBACK] Guardrail ativado após %d iterações — "
        "mensagem de fallback injetada no histórico",
        state["iterations"],
    )

    return {"messages": [clean_msg]}


# ===========================================================================
# Roteador condicional local
#
# Versão expandida do route_after_agent (nodes.py) que inclui o destino
# "fallback" explícito. Mantida aqui (e não em nodes.py) para não alterar
# o contrato do Passo 3 — nodes.py permanece independente do grafo.
# ===========================================================================

def _route(state: AgentState) -> Literal["tools", "fallback", "__end__"]:
    """
    Roteador condicional pós-agent com três saídas possíveis.

    Camada 1 — Resposta final:
      Sem tool_calls na última mensagem → END.

    Camada 2 — Guardrail (exaustão cognitiva):
      Com tool_calls E iterations >= MAX_ITERATIONS → "fallback".
      O fallback_node limpa o estado antes de encerrar.

    Camada 3 — Ciclo ReAct normal:
      Com tool_calls E iterations < MAX_ITERATIONS → "tools".

    Args:
        state: AgentState atualizado pelo agent_node.

    Returns:
        Literal["tools", "fallback", "__end__"]
    """
    last_message: AIMessage = state["messages"][-1]
    tool_calls: list = getattr(last_message, "tool_calls", []) or []

    if not tool_calls:
        _logger.info("✅ [ROUTE] Sem tool_calls → END")
        return "__end__"

    if state["iterations"] >= MAX_ITERATIONS:
        _logger.warning(
            "🚨 [ROUTE] Guardrail → fallback | iterations=%d/%d | "
            "tool_calls pendentes=%d",
            state["iterations"],
            MAX_ITERATIONS,
            len(tool_calls),
        )
        return "fallback"

    _logger.info(
        "🔧 [ROUTE] %d tool_call(s) → tools | iteração %d/%d",
        len(tool_calls),
        state["iterations"],
        MAX_ITERATIONS,
    )
    return "tools"


# ===========================================================================
# Factory pública — cria e compila o grafo para uma requisição específica
# ===========================================================================

def create_agent_graph(jwt_token: str):
    """
    Instancia, monta e compila o StateGraph para uma requisição específica.

    Por que recria o grafo por request e não usa um singleton global?
      O grafo encapsula ferramentas de API autenticadas com o JWT do usuário
      via closure (make_nodes → make_api_tools). O JWT tem validade curta e é
      único por usuário — um grafo singleton compartilharia o JWT entre requests,
      o que seria uma falha de segurança crítica.

      O custo de recriar o grafo é mínimo (~ms): o StateGraph é um objeto leve.
      O estado persistente (histórico de conversas) vive no _memory (MemorySaver),
      que é singleton e sobrevive entre requests do mesmo usuário.

    Topologia montada:
      START → inject_context → agent → fix_agent_output ───────────────────→ END
                                                        └─ tool_calls → tools → agent ...
                                                        └─ guardrail → fallback ──────→ END

    Nota sobre inject_context:
      O spec do Passo 4 define o entry_point como "agent". Este módulo inclui
      inject_context ANTES de agent (START → inject_context → agent) porque:
        1. O nó foi projetado no Passo 3 especificamente para ser o ponto de
           entrada, hidratando context_data com o payload do backend .NET.
        2. inject_context é tolerante a falhas (context_payload ausente = no-op),
           então nunca bloqueia o fluxo se o .NET não enviar o payload.
        3. Mantém a arquitetura coerente com o design dos passos anteriores.

    Args:
        jwt_token: JWT Bearer do usuário, extraído e validado pelo .NET.

    Returns:
        CompiledGraph pronto para .invoke() ou .ainvoke().
    """
    # ── Instancia os nós via factory (JWT baked-in via closure) ─────────────
    agent_node, tool_node = make_nodes(jwt_token)

    # ── Constrói o StateGraph ────────────────────────────────────────────────
    workflow = StateGraph(AgentState)

    # Registro dos nós
    workflow.add_node("inject_context", inject_context)
    workflow.add_node("agent", agent_node)
    workflow.add_node("fix_agent_output", fix_agent_output)
    workflow.add_node("tools", tool_node)
    workflow.add_node("fallback", fallback_node)

    # Ponto de entrada: inject_context popula context_data antes do primeiro ReAct
    workflow.add_edge(START, "inject_context")
    workflow.add_edge("inject_context", "agent")

    # Camada 1 anti-leak: toda saída do LLM passa pelo interceptor antes do roteador.
    # fix_agent_output converte tool_calls textuais em AIMessage estruturada (no-op
    # quando o modelo emite corretamente).
    workflow.add_edge("agent", "fix_agent_output")

    # Aresta condicional: decisão após o interceptor garantir formato correto
    workflow.add_conditional_edges(
        "fix_agent_output",
        _route,
        {
            "tools":    "tools",    # há tool_calls + budget → executa ferramentas
            "fallback": "fallback", # guardrail ativado → limpa estado e encerra
            "__end__":  END,        # resposta final gerada → encerra normalmente
        },
    )

    # Fechamento do loop ReAct: após executar ferramentas, volta ao raciocínio
    workflow.add_edge("tools", "agent")

    # Saída do fallback: após injetar mensagem amigável, encerra o grafo
    workflow.add_edge("fallback", END)

    # Compila com checkpointer de memória para persistir histórico por thread_id
    return workflow.compile(checkpointer=_memory)
