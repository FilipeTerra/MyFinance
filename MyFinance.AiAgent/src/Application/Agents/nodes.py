"""
nodes.py — Nós e Lógica de Roteamento do Grafo LangGraph

Este módulo define os nós (nodes) que compõem o grafo. A construção do
StateGraph e o roteador condicional estão em graph.py.

Responsabilidades:
  - inject_context : hidrata AgentState com o pacote enviado pelo backend .NET.
  - make_nodes     : factory que produz agent_node + tool_node via closure JWT.

Decisões de performance (correções críticas):
  C1. agent_node é ASYNC e usa await _llm.ainvoke() — a chamada ao LLM é a
      operação mais longa do sistema; com invoke() síncrono ela bloqueava o
      event loop inteiro (nenhum outro usuário era atendido durante a geração).
  C2. ChatOllama com temperature=0 e num_ctx=8192 — o default do Ollama é
      temperature≈0.8 (tool_calls instáveis/malformados) e num_ctx=2048
      (o system prompt era truncado silenciosamente em conversas longas).
  C3. Trimming de histórico — o MemorySaver acumula a conversa inteira;
      _trim_history limita o que é ENVIADO ao LLM às últimas N interações,
      sempre começando em uma HumanMessage (nunca corta um turno no meio).
"""

import logging
from datetime import datetime, timezone

from langchain_core.messages import AIMessage, BaseMessage, HumanMessage, SystemMessage
from langchain_core.runnables import RunnableConfig
from langgraph.prebuilt import ToolNode

from src.Application.Agents.state import AgentState, ContextData, MAX_ITERATIONS
from src.Application.Agents.Tools.tools import MATH_TOOLS
from src.Application.Agents.Tools.financial_tools import consultar_teoria_financeira
from src.Application.Agents.Tools.api_tools import make_api_tools
from src.Application.Agents.Tools.investment_tools import QUANT_TOOLS
from src.Infra.Llm.ollama_provider import get_chat_llm, ainvoke_with_retry

_logger = logging.getLogger("myfinance.agent")

# ---------------------------------------------------------------------------
# Parâmetros de geração e de janela de contexto
# ---------------------------------------------------------------------------

# temperature=0: agente de tool-calling precisa de saída determinística.
# O default do Ollama (~0.8) é uma causa direta de JSON malformado em tool_calls.
_LLM_TEMPERATURE = 0.0

# num_ctx=8192: default do Ollama é 2048 tokens — com system prompt + contexto
# financeiro + histórico, o início do prompt (as regras!) era truncado em silêncio.
_LLM_NUM_CTX = 8192

# Quantos turnos de usuário (HumanMessage) são enviados ao LLM por invocação.
# O histórico completo permanece no MemorySaver; isto limita apenas a janela
# visível pelo modelo, evitando estouro de contexto em conversas longas.
_MAX_HISTORY_TURNS = 5

# Timeout do cliente HTTP do Ollama (segundos). Sem isso, um provedor travado
# prende a requisição até o timeout de 10 min do HttpClient do .NET.
_LLM_TIMEOUT_S = 120.0


# ===========================================================================
# System Prompt — caminho de decisão + formato de resposta conversacional
#
# Estruturado em 3 blocos para um modelo pequeno (3b):
#   COMO AGIR    → tabela intenção→ferramenta (caminho bem definido)
#   REGRAS       → restrições operacionais (anti-alucinação)
#   FORMATO      → conversa fluida (não relatório em blocos rígidos),
#                  sempre fechando com uma pergunta que puxa o próximo passo
# Detalhes de cada ferramenta ficam nas docstrings (enviadas via bind_tools);
# aqui fica apenas o roteamento de intenção, curto e sem duplicação.
# ===========================================================================

_SYSTEM_PROMPT = (
    "Você é o Claudio, assistente financeiro pessoal do usuário. "
    "Responda SEMPRE em português do Brasil. Seja objetivo e orientado à ação.\n"
    "\n"
    "== COMO AGIR ==\n"
    "Identifique a intenção do usuário e siga o caminho correspondente:\n"
    "1. Saldo ou contas → os dados já estão no CONTEXTO abaixo; "
    "use consultar_saldos_contas apenas se o contexto estiver vazio.\n"
    "2. Metas (progresso, status) → CONTEXTO; se vazio, consultar_metas_financeiras.\n"
    "3. Análise geral de gastos ('onde gasto mais?') → chame "
    "analisar_gastos_por_categoria E calcular_resumo_financeiro (mesmo período).\n"
    "4. Extrato / transações específicas → consultar_transacoes_recentes.\n"
    "5. Gasto de UMA categoria mês a mês (uber, alimentação) → relatorio_mensal_por_categoria.\n"
    "6. Simular investimento/rendimento → se o usuário não informou a taxa, chame "
    "buscar_taxa_selic primeiro; depois simular_investimento.\n"
    "7. Financiamento/parcelas → calcular_juros_financiamento.\n"
    "8. Ação da B3 / valuation → consultar_indicadores_b3 (ticker de 4-6 letras; "
    "se o usuário deu o nome da empresa, deduza o ticker).\n"
    "9. Conselho ou teoria financeira (50/30/20, reserva de emergência, juros "
    "compostos, ativos vs passivos) → consultar_teoria_financeira antes de responder.\n"
    "10. Criar meta / organizar sobra do salário → PROCESSO EM 3 PASSOS: "
    "(a) chame simular_meta_ideal; "
    "(b) apresente a proposta (valor alvo, aporte, prazo) e pergunte "
    "'Posso criar essa meta no sistema para você agora?' e PARE — não crie ainda; "
    "(c) somente se o usuário confirmar, chame criar_meta_financeira com os valores simulados.\n"
    "11. Impacto de assumir uma nova despesa → simular_impacto_nova_despesa.\n"
    "12. Aporte em meta existente → realizar_aporte_meta (confirme os detalhes depois).\n"
    "\n"
    "== REGRAS ==\n"
    "• Máximo de 3 chamadas de ferramenta por vez.\n"
    "• Períodos: converta para data_inicio/data_fim no formato YYYY-MM-DD "
    "(ex: 'maio de 2026' → 2026-05-01 a 2026-05-31). "
    "Se o usuário não mencionar período, OMITA esses parâmetros.\n"
    "• NUNCA calcule de cabeça — use as ferramentas de cálculo.\n"
    "• NUNCA invente números, datas ou dados. Se uma ferramenta falhar ou não "
    "retornar dados, diga isso claramente ao usuário.\n"
    "• OBRIGATÓRIO: quando uma ferramenta retornar valores, sua resposta deve "
    "citar pelo menos 2 ou 3 deles com o símbolo R$ (os mais relevantes para a "
    "pergunta) — NUNCA responda apenas com a pergunta final sem antes "
    "apresentar os números que o usuário pediu.\n"
    "\n"
    "== FORMATO DA RESPOSTA ==\n"
    "Responda como numa conversa natural, não como um relatório robótico:\n"
    "• Vá direto ao ponto: a PRIMEIRA frase já deve responder o que foi "
    "perguntado, com os números mais importantes integrados ao texto (com R$) "
    "— nunca omita valores, mas não os empilhe em blocos separados por título.\n"
    "• Mencione o que for relevante do fluxo de caixa, indicadores ou "
    "destaques em 1-3 frases curtas e fluidas, com **negrito** nos valores. "
    "Se precisar de lista, mantenha os itens colados, sem linha em branco entre eles.\n"
    "• Feche com 1 frase curta de análise/insight.\n"
    "• TERMINE SEMPRE com uma pergunta curta que puxe a conversa adiante — "
    "sugerindo uma análise relacionada (ex: outro período, outra categoria, "
    "comparar com o mês anterior) ou perguntando se quer entender algum ponto "
    "mais a fundo. Nunca finalize a resposta sem essa pergunta.\n"
    "\n"
    "Exemplo de tom (não copie os números, é só o estilo):\n"
    "\"Em maio você teve **R$ 2.500** de receita e gastou **R$ 650**, então "
    "sobrou **R$ 1.850** no período — além disso, guardou **R$ 400** numa "
    "meta. A categoria que mais pesou foi *Alimentação* (R$ 450). Quer que eu "
    "veja como ficou junho ou prefere entender melhor esses gastos com alimentação?\"\n"
    "\n"
    "NUNCA use os títulos '📊 Dados', '💡 Análise', '✅ Próximas ações' ou "
    "qualquer rótulo de seção. Nunca liste 'próximas ações' como itens "
    "separados — a sugestão de próximo passo é a pergunta final. "
    "Nunca mencione nomes de ferramentas, JSON ou detalhes técnicos."
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
# Helper privado: janela de histórico enviada ao LLM (correção C3)
# ===========================================================================

def _trim_history(messages: list[BaseMessage], max_turns: int = _MAX_HISTORY_TURNS) -> list[BaseMessage]:
    """
    Limita o histórico enviado ao LLM às últimas `max_turns` interações do usuário.

    Por que por TURNOS e não por número de mensagens:
      Um turno é [HumanMessage, AIMessage(tool_calls), ToolMessage..., AIMessage].
      Cortar no meio de um turno deixaria ToolMessages órfãs no início da janela
      (sem a AIMessage com os tool_calls correspondentes), o que confunde o modelo
      e quebra provedores que validam o encadeamento de mensagens.

    A janela sempre começa em uma HumanMessage. O turno corrente nunca é cortado,
    pois é sempre o último. O histórico COMPLETO permanece intocado no MemorySaver —
    isto afeta apenas o que o modelo enxerga nesta invocação.
    """
    human_indices = [i for i, m in enumerate(messages) if isinstance(m, HumanMessage)]
    if len(human_indices) <= max_turns:
        return list(messages)

    start = human_indices[-max_turns]
    trimmed = list(messages[start:])
    _logger.info(
        "✂️  [TRIM] Histórico: %d → %d mensagens (janela de %d turnos)",
        len(messages), len(trimmed), max_turns,
    )
    return trimmed


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
    # Ordem: matemática pura → quant/B3 → conhecimento RAG → API .NET (autenticada)
    api_tools = make_api_tools(jwt_token)
    all_tools = MATH_TOOLS + QUANT_TOOLS + [consultar_teoria_financeira] + api_tools

    _logger.info(
        "⚙️  [NODES] %d ferramentas registradas: %s",
        len(all_tools),
        [t.name for t in all_tools],
    )

    # ── LLM com ferramentas vinculadas — instanciado UMA VEZ por request ────
    # temperature=0 e num_ctx=8192: ver constantes no topo do módulo (C2).
    # A construção (resolução de modelo/endpoint + timeout no httpx interno) é
    # centralizada em get_chat_llm — geração que exceder _LLM_TIMEOUT_S falha
    # rápido em vez de pendurar a requisição indefinidamente.
    _llm = get_chat_llm(
        "chat",
        temperature=_LLM_TEMPERATURE,
        num_ctx=_LLM_NUM_CTX,
        timeout=_LLM_TIMEOUT_S,
    ).bind_tools(all_tools)

    # ── Nó 3: tool_node ─────────────────────────────────────────────────────
    # ToolNode nativo do LangGraph:
    #   - Recebe AIMessage com tool_calls (Thought do agente).
    #   - Despacha para a função Python correspondente (Action) — ferramentas
    #     async são aguardadas nativamente quando o grafo roda via ainvoke.
    #   - Retorna uma ou mais ToolMessages com os resultados (Observation).
    #   - Captura exceções de ferramenta e retorna ToolMessage de erro sem
    #     crashar o grafo — o agente vê o erro e pode tentar outra estratégia.
    tool_node = ToolNode(all_tools)

    # ── Nó 2: agent_node (ASYNC — correção C1) ──────────────────────────────
    async def agent_node(state: AgentState, config: RunnableConfig) -> dict:
        """
        Nó de raciocínio (Thought) do ciclo ReAct.

        A cada chamada, este nó:
          1. Monta o system prompt final (base + data de hoje + contexto financeiro).
          2. Aplica a janela de histórico (_trim_history) sobre as mensagens.
          3. Invoca o LLM de forma ASSÍNCRONA (await ainvoke) — não bloqueia
             o event loop; outros usuários seguem sendo atendidos em paralelo.
          4. Retorna a resposta como nova mensagem e incrementa iterations.

        O LLM pode retornar:
          a) AIMessage sem tool_calls → resposta final ao usuário.
          b) AIMessage com tool_calls → solicitação de ação ao tool_node.
        """
        context = state.get("context_data", {})
        new_iteration = state["iterations"] + 1

        # Data corrente: essencial para o modelo converter períodos relativos
        # ('mês passado', 'este ano') em data_inicio/data_fim corretos.
        hoje = datetime.now(timezone.utc).strftime("%d/%m/%Y")
        enriched_system = (
            f"{_SYSTEM_PROMPT}\n\nHoje é {hoje}."
            + _format_context_block(context)
        )

        history = _trim_history(list(state["messages"]))
        messages_to_send = [SystemMessage(content=enriched_system)] + history

        _logger.info(
            "🤖 [AGENT] iteração=%d/%d | janela=%d msgs",
            new_iteration,
            MAX_ITERATIONS,
            len(history),
        )

        # await + ainvoke: a geração do LLM roda sem travar o event loop.
        # O RunnableConfig é propagado para callbacks (logger, tracer).
        # Retry único (via ainvoke_with_retry): falhas transitórias (rede,
        # provedor reiniciando) são comuns com Ollama; falha dupla propaga.
        response: AIMessage = await ainvoke_with_retry(
            _llm, messages_to_send, config=config, label="AGENT"
        )

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
