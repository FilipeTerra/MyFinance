"""
nodes.py — Nós e Lógica de Roteamento do Grafo LangGraph

Passo 3 da refatoração para Workflow Determinístico com ciclo ReAct.

Este módulo define os nós (nodes) e o roteador condicional (edge) que compõem
o grafo. A construção do StateGraph em si está no Passo 4 (graph.py).

Responsabilidades:
  - inject_context   : hidrata AgentState com o pacote enviado pelo backend .NET.
  - make_nodes       : factory que produz agent_node + tool_node via closure JWT.
  - route_after_agent: guardrail operacional + roteador condicional pós-agent.
"""

import logging
from typing import Literal

from langchain_core.messages import AIMessage, SystemMessage
from langchain_core.runnables import RunnableConfig
from langchain_ollama import ChatOllama
from langgraph.prebuilt import ToolNode

from src.Application.Agents.state import AgentState, ContextData, MAX_ITERATIONS
from src.Application.Agents.Tools.tools import MATH_TOOLS
from src.Application.Agents.Tools.financial_tools import consultar_teoria_financeira
from src.Application.Agents.Tools.api_tools import make_api_tools
from src.Application.Agents.Tools.investment_tools import QUANT_TOOLS
from src.Infra.Llm.ollama_provider import get_ollama_config, get_model

_logger = logging.getLogger("myfinance.agent")


# ===========================================================================
# System Prompt Base
# Centralizado aqui: nodes.py é o único módulo que invoca o LLM diretamente.
# O bloco de contexto financeiro (context_data) é anexado dinamicamente
# por _format_context_block antes de cada invocação.
# ===========================================================================

_SYSTEM_PROMPT = (
    "⚠️ REGRA DE SEGURANÇA (CRÍTICA): NUNCA chame mais de 3 ferramentas na mesma iteração. "
    "Se o usuário fizer uma pergunta ampla sobre seus gastos ou pedir uma análise geral, "
    "priorize EXCLUSIVAMENTE o uso das ferramentas analisar_gastos_por_categoria ou "
    "calcular_resumo_financeiro. NUNCA tente puxar listas longas de transações para análises amplas. "

    "Você é o Claudio, o assistente financeiro pessoal do usuário. "
    "IDIOMA OBRIGATÓRIO: Responda SEMPRE em português do Brasil, sem exceção. "
    "Seja EXTREMAMENTE objetivo, moderno e orientado à ação. "

    "Regras de formatação: "
    "1. Sem introduções longas. Vá direto ao ponto. "
    "2. Use emojis estratégicos (📊, 🎯, 🛡️, 💰) para destacar tópicos. "
    "3. Prefira listas e negrito a parágrafos corridos. "
    "4. OBRIGATÓRIO: quando uma ferramenta retornar dados (saldo, receitas, despesas, metas, "
    "   simulações), SEMPRE apresente TODOS os valores retornados antes de qualquer análise. "
    "   Nunca omita números, percentuais ou datas vindos das ferramentas. "
    "5. Sempre finalize sugerindo 2 a 3 ações concretas que o usuário pode tomar agora. "

    "Regras de uso de ferramentas: "
    "5. Para simular rendimentos futuros ou projetar investimentos, use SEMPRE "
    "   simular_investimento. Se o usuário não informar a taxa, chame buscar_taxa_selic "
    "   primeiro para obter SELIC/CDI atuais. NUNCA calcule de cabeça. "
    "6. Para calcular parcelas, custo total ou juros de um financiamento, use "
    "   calcular_juros_financiamento (Tabela Price). "
    "7. Para comparar quitar dívida vs investir, use comparar_quitacao_vs_investimento "
    "   SOMENTE se o usuário já informou AMBAS as taxas. Caso contrário, pergunte primeiro. "
    "8. Para relatório mês a mês de uma categoria (uber, alimentação, transporte), use "
    "   relatorio_mensal_por_categoria(filtro_categoria=..., ultimos_meses=...). "
    "9. Para análise geral de gastos sem filtro, chame AMBAS: analisar_gastos_por_categoria "
    "   E calcular_resumo_financeiro com o mesmo número de dias. "
    "10. Para qualquer conselho, princípio ou teoria financeira (50/30/20, reserva de emergência, "
    "    independência financeira, juros compostos, ativos vs passivos), use SEMPRE "
    "    consultar_teoria_financeira antes de responder. Cite os conceitos dos livros. "
    "11. Só crie metas com criar_meta_financeira quando solicitado EXPLICITAMENTE. "
    "12. Após qualquer ação de criação ou aporte, confirme os detalhes ao usuário. "

    "13. Quando o usuário mencionar um mês ou período específico (ex: 'maio de 2026', "
    "    'primeiro trimestre', 'semana passada', 'de janeiro a março'), converta para "
    "    data_inicio e data_fim no formato YYYY-MM-DD e passe às ferramentas de análise. "
    "    Exemplos de conversão: "
    "    'maio de 2026' → data_inicio='2026-05-01', data_fim='2026-05-31'; "
    "    'primeiro trimestre de 2026' → data_inicio='2026-01-01', data_fim='2026-03-31'; "
    "    'junho' (mês corrente sem ano) → data_inicio='2026-06-01', data_fim='2026-06-30'. "
    "    Se o usuário não mencionar nenhum período, omita data_inicio e data_fim "
    "    (as ferramentas usam os últimos 30 dias por padrão). "
    
    "14. Para analisar ações da bolsa brasileira (B3), avaliar valuation ou "
        "discutir se uma empresa está barata/cara, use SEMPRE a ferramenta "
        "consultar_indicadores_b3. Se o usuário falar o nome de uma empresa "
        "sem informar o ticker, identifique o ticker correspondente de 4 a 6 letras "
        "antes de chamar a ferramenta."

    "Nunca invente dados nem faça cálculos mentais. "
    "Nunca mencione nomes de ferramentas ou detalhes técnicos ao usuário."
)


# ===========================================================================
# Helper privado: formata context_data como bloco textual para o prompt
# ===========================================================================

def _format_context_block(context: ContextData) -> str:
    """
    Serializa os campos de context_data num bloco de texto estruturado que
    é anexado ao final do _SYSTEM_PROMPT antes de cada invocação do LLM.

    Benefícios:
      - O LLM 'vê' saldos, metas e resumo do mês desde a primeira token.
      - Perguntas básicas ('qual meu saldo?', 'como está minha meta?') são
        respondidas sem chamar ferramentas de lookup, reduzindo latência.
      - Nós de ação (ex: realizar_aporte_meta) podem atualizar context_data
        no estado, e o próximo ciclo ReAct terá o contexto refletindo a mudança.

    Retorna string vazia se context estiver vazio, sem alterar o prompt base.
    """
    if not context:
        return ""

    lines = ["\n\n=== CONTEXTO FINANCEIRO ATUAL DO USUÁRIO ==="]

    accounts = context.get("account_snapshots", [])
    if accounts:
        lines.append("Contas e saldos:")
        for acc in accounts:
            lines.append(
                f"  • [{acc.get('account_type', 'Conta')}] "
                f"{acc.get('name', 'Sem nome')}: "
                f"R$ {acc.get('balance', 0.0):,.2f}"
            )

    summary = context.get("monthly_summary", {})
    if summary:
        lines.append(
            f"Mês corrente — "
            f"Receitas: R$ {summary.get('totalIncome', 0.0):,.2f} | "
            f"Despesas: R$ {summary.get('totalExpenses', 0.0):,.2f} | "
            f"Líquido: R$ {summary.get('netBalance', 0.0):,.2f}"
        )

    gamification = context.get("gamification", {})
    goals = gamification.get("active_goals", [])
    if goals:
        lines.append("Metas ativas:")
        for g in goals:
            lines.append(
                f"  • {g.get('name', '?')}: "
                f"R$ {g.get('current_amount', 0.0):,.2f} / "
                f"R$ {g.get('target_amount', 0.0):,.2f} "
                f"({g.get('progress_pct', 0.0):.1f}%)"
            )

    if level := gamification.get("level"):
        lines.append(f"Nível de engajamento: {level}")

    lines.append("=== FIM DO CONTEXTO ===")
    return "\n".join(lines)


# ===========================================================================
# Nó 1: inject_context
# ===========================================================================

def inject_context(state: AgentState, config: RunnableConfig) -> dict:
    """
    Primeiro nó do grafo — popula context_data com o payload do backend .NET.

    O .NET envia um dict em config["configurable"]["context_payload"] com chaves
    camelCase (convenção JSON do C#). Este nó normaliza para snake_case e faz
    merge apenas dos campos presentes, respeitando o total=False do ContextData.

    Tolerância a falhas:
      - Se context_payload estiver ausente, retorna context_data vazio.
      - O agente funciona normalmente sem contexto pré-carregado; apenas
        fará mais chamadas às ferramentas de lookup para obter os mesmos dados.

    Mapeamento camelCase → snake_case:
      userId             → user_id
      accountSnapshots   → account_snapshots
      recentTransactions → recent_transactions
      gamification       → gamification  (mesmo nome, sem conversão)
      monthlySummary     → monthly_summary

    Args:
        state : AgentState — não lido neste nó, presente por conformidade de assinatura.
        config: RunnableConfig com configurable["context_payload"] opcional.

    Returns:
        dict parcial {"context_data": ContextData} para merge no AgentState.
    """
    raw: dict = config.get("configurable", {}).get("context_payload", {})
    context: ContextData = {}

    if user_id := raw.get("userId"):
        context["user_id"] = user_id
    if accounts := raw.get("accountSnapshots"):
        context["account_snapshots"] = accounts
    if transactions := raw.get("recentTransactions"):
        context["recent_transactions"] = transactions
    if gamification := raw.get("gamification"):
        context["gamification"] = gamification
    if summary := raw.get("monthlySummary"):
        context["monthly_summary"] = summary

    _logger.info(
        "📦 [INJECT] user=%s | contas=%d | metas=%d | contexto preenchido",
        context.get("user_id", "anônimo")[:8],
        len(context.get("account_snapshots", [])),
        len(context.get("gamification", {}).get("active_goals", [])),
    )

    return {"context_data": context}


# ===========================================================================
# Factory: make_nodes
# ===========================================================================

def make_nodes(jwt_token: str) -> tuple:
    """
    Factory que constrói agent_node e tool_node para uma requisição específica.

    Por que factory em vez de funções top-level?

    1. JWT por request: make_api_tools(jwt_token) gera ferramentas HTTP com o
       token do usuário baked-in via closure. O JWT é único por request.

    2. LLM criado uma vez: O ChatOllama + bind_tools ocorre aqui, não dentro do
       agent_node. No ciclo ReAct, agent_node pode ser chamado 3–10 vezes por
       request. Recriar o objeto LLM a cada chamada seria desnecessário.

    3. Lista unificada de ferramentas: o ToolNode e o bind_tools do LLM devem
       receber EXATAMENTE a mesma lista, ou o LLM pode gerar tool_calls para
       ferramentas que o ToolNode não conhece, causando KeyError.

    Args:
        jwt_token: JWT Bearer extraído e validado pelo .NET antes de chegar aqui.

    Returns:
        (agent_node, tool_node): callables prontos para registro no StateGraph.
    """
    # ── Lista unificada de ferramentas ───────────────────────────────────────
    # Ordem: matemática pura → conhecimento RAG → API .NET (autenticada)
    api_tools = make_api_tools(jwt_token)
    all_tools = [
        t for t in MATH_TOOLS + QUANT_TOOLS + [consultar_teoria_financeira] + api_tools
    ]

    _logger.info(
        "⚙️  [NODES] %d ferramentas registradas: %s",
        len(all_tools),
        [t.name for t in all_tools],
    )

    # ── LLM com ferramentas vinculadas — instanciado UMA VEZ por request ────
    # get_model / get_ollama_config resolvem o provedor ativo (local ou remoto)
    # e cachearão o resultado por 60 s (health check TTL do ollama_provider).
    model_name = get_model("chat")
    _llm = ChatOllama(
        model=model_name,
        **get_ollama_config(),
    ).bind_tools(all_tools)

    # ── Nó 3: tool_node ─────────────────────────────────────────────────────
    # ToolNode nativo do LangGraph:
    #   - Recebe AIMessage com tool_calls (Thought do agente).
    #   - Despacha para a função Python correspondente (Action).
    #   - Retorna uma ou mais ToolMessages com os resultados (Observation).
    #   - Captura exceções de ferramenta e retorna ToolMessage de erro sem
    #     crashar o grafo — o agente vê o erro e pode tentar outra estratégia.
    tool_node = ToolNode(all_tools)

    # ── Nó 2: agent_node ────────────────────────────────────────────────────
    def agent_node(state: AgentState, config: RunnableConfig) -> dict:
        """
        Nó de raciocínio (Thought) do ciclo ReAct.

        A cada chamada, este nó:
          1. Monta o system prompt final (base + bloco de contexto financeiro).
          2. Invoca o LLM com o histórico completo de mensagens.
          3. Retorna a resposta como nova mensagem e incrementa iterations.

        O LLM pode retornar:
          a) AIMessage sem tool_calls → resposta final ao usuário.
          b) AIMessage com tool_calls → solicitação de ação ao tool_node.

        IMPORTANTE sobre iterations:
          O campo não usa reducer no AgentState (ver state.py, linha ~137).
          O LangGraph substitui o valor diretamente com o que retornamos.
          Portanto, retornamos o VALOR ABSOLUTO já incrementado:
            {"iterations": state["iterations"] + 1}
          e NÃO o delta {"iterations": 1} — que produziria o mesmo efeito
          mas é semânticamente errado sem um reducer de soma.

        Args:
            state : AgentState com histórico e contexto atual.
            config: RunnableConfig — pode carregar callbacks (AgentCallbackLogger),
                    tags e metadados que são propagados ao LLM via .invoke(config=).

        Returns:
            dict parcial {"messages": [AIMessage], "iterations": int}.
        """
        context = state.get("context_data", {})
        new_iteration = state["iterations"] + 1

        # System prompt enriquecido com o snapshot financeiro do usuário
        enriched_system = _SYSTEM_PROMPT + _format_context_block(context)
        messages_to_send = [SystemMessage(content=enriched_system)] + list(state["messages"])

        _logger.info(
            "🤖 [AGENT] iteração=%d/%d | hist=%d msgs",
            new_iteration,
            MAX_ITERATIONS,
            len(state["messages"]),
        )

        # Propaga o RunnableConfig ao LLM para que callbacks (logger, tracer)
        # registrem esta invocação com o contexto correto da thread.
        response: AIMessage = _llm.invoke(messages_to_send, config=config)

        tool_calls = getattr(response, "tool_calls", []) or []
        _logger.info(
            "🤖 [AGENT] concluído | tool_calls=%d%s",
            len(tool_calls),
            f" → {[tc['name'] for tc in tool_calls]}" if tool_calls else " → resposta final",
        )

        return {
            "messages": [response],
            "iterations": new_iteration,
        }

    return agent_node, tool_node


# ===========================================================================
# Roteador condicional
# ===========================================================================

def route_after_agent(state: AgentState) -> Literal["tools", "__end__"]:
    """
    Guardrail operacional e roteador condicional.

    Executado após cada passagem pelo nó agent para decidir o próximo
    destino no grafo. Atua como a 'consciência' do ciclo ReAct em duas camadas:

    ┌─────────────────────────────────────────────────────────────────────┐
    │  CAMADA 1 — Verificação de tool_calls                               │
    │                                                                     │
    │  Se a AIMessage retornada pelo LLM não contém tool_calls, o agente  │
    │  chegou à sua resposta final natural → roteamento para END.         │
    ├─────────────────────────────────────────────────────────────────────┤
    │  CAMADA 2 — Guardrail contra exaustão cognitiva                     │
    │                                                                     │
    │  Um LLM pode entrar em loop quando:                                 │
    │    • Uma ferramenta retorna erro repetidamente (ex: API offline).   │
    │    • O LLM raciocina em círculo sem chegar a uma conclusão.         │
    │    • Tool_calls mal formados são gerados e re-tentados infinitamente.│
    │                                                                     │
    │  Se iterations >= MAX_ITERATIONS, mesmo havendo tool_calls, o grafo │
    │  é encerrado para evitar consumo indefinido de tokens e degradação  │
    │  da experiência do usuário (exaustão cognitiva do agente).          │
    │                                                                     │
    │  NOTA para o Passo 4 (graph.py):                                    │
    │  Quando este guardrail dispara, a última mensagem no estado ainda   │
    │  contém tool_calls não resolvidos. Adicione um nó `fallback_node`   │
    │  antes de END que injeta uma AIMessage de fallback amigável,        │
    │  garantindo que o usuário receba uma resposta controlada.           │
    └─────────────────────────────────────────────────────────────────────┘

    Args:
        state: AgentState com o histórico atualizado pelo agent_node.

    Returns:
        "tools"    — há tool_calls E iterations < MAX_ITERATIONS.
        "__end__"  — sem tool_calls (resposta final gerada normalmente).
        "__end__"  — guardrail ativado (iterations >= MAX_ITERATIONS).
    """
    last_message: AIMessage = state["messages"][-1]
    tool_calls: list = getattr(last_message, "tool_calls", []) or []

    # ── Camada 1: sem chamadas de ferramentas ────────────────────────────────
    if not tool_calls:
        _logger.info("✅ [ROUTER] Sem tool_calls → END (resposta final)")
        return "__end__"

    # ── Camada 2: guardrail de iterações ─────────────────────────────────────
    if state["iterations"] >= MAX_ITERATIONS:
        _logger.warning(
            "🚨 [ROUTER] Guardrail ativado | iterations=%d/%d | "
            "%d tool_call(s) pendente(s) descartados → END forçado",
            state["iterations"],
            MAX_ITERATIONS,
            len(tool_calls),
        )
        return "__end__"

    # ── Budget OK: executa ferramentas ───────────────────────────────────────
    _logger.info(
        "🔧 [ROUTER] %d tool_call(s) → tools | iteração %d/%d",
        len(tool_calls),
        state["iterations"],
        MAX_ITERATIONS,
    )
    return "tools"
