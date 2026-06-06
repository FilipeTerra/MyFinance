"""
state.py — Estado Compartilhado do Grafo LangGraph

Passo 1 da refatoração para Workflow Determinístico com ciclo ReAct.

No LangGraph, todos os nós (nodes) leem e escrevem neste único objeto de estado.
Nenhum nó passa dados diretamente para outro; toda comunicação acontece
através deste estado centralizado, o que torna o fluxo auditável e previsível.
"""
import operator
from typing import Annotated, Any, Sequence, TypedDict

from langchain_core.messages import BaseMessage
from langgraph.graph.message import add_messages

# ---------------------------------------------------------------------------
# Constante de segurança — usada pelo nó de roteamento (router node)
# ---------------------------------------------------------------------------

# Número máximo de ciclos ReAct (Thought → Action → Observation) permitidos
# por invocação. Ao atingir este limite, o grafo encerra com mensagem controlada
# em vez de entrar em loop infinito por falha de ferramenta ou raciocínio circular.
MAX_ITERATIONS = 10


# ---------------------------------------------------------------------------
# Sub-tipagem do Pacote de Contexto enviado pelo backend .NET
# ---------------------------------------------------------------------------

class AccountSnapshot(TypedDict, total=False):
    """
    Fotografia do estado de uma conta no momento em que a sessão foi iniciada.
    Evita uma chamada extra à API para buscar saldos no início da conversa.
    """
    account_id: str
    name: str
    balance: float
    account_type: str   # "Checking" | "Savings" | "Investment" | "CreditCard"
    currency: str       # "BRL" por padrão


class ActiveGoal(TypedDict, total=False):
    """Representação resumida de uma meta financeira ativa do usuário."""
    goal_id: str
    name: str
    target_amount: float
    current_amount: float
    deadline: str       # ISO 8601 (ex: "2025-12-31")
    progress_pct: float # 0.0 a 100.0


class GamificationProfile(TypedDict, total=False):
    """
    Perfil de gamificação do usuário: metas, pontos e nível atual.
    Permite que o agente personalize respostas com base no engajamento do usuário
    (ex: parabenizar por progresso numa meta sem precisar consultar a API).
    """
    active_goals: list[ActiveGoal]
    total_points: int
    level: str          # ex: "Poupador Iniciante", "Investidor Estratégico"


class ContextData(TypedDict, total=False):
    """
    Pacote de Contexto pré-carregado enviado pelo backend .NET ao iniciar
    a sessão de chat. Funciona como um 'warm-up' do agente: ele já conhece
    o estado financeiro básico do usuário desde o primeiro token gerado,
    sem precisar chamar ferramentas de lookup para dados que o .NET já tem.

    O campo `total=False` (todos opcionais) permite que o estado seja inicializado
    de forma incremental — campos são populados à medida que ficam disponíveis,
    e nós de atualização podem sobrescrever campos específicos após ações
    que mudam o estado financeiro (ex: após um aporte, atualizar account_snapshots).
    """
    user_id: str                                    # sub do JWT, chave de todo o contexto
    account_snapshots: list[AccountSnapshot]        # saldos e tipos de todas as contas
    recent_transactions: list[dict[str, Any]]       # últimas N transações (enriquecimento imediato)
    gamification: GamificationProfile               # metas ativas e nível de engajamento
    monthly_summary: dict[str, Any]                 # resumo do mês corrente (receita, despesa, saldo líquido)


# ---------------------------------------------------------------------------
# Estado Principal do Grafo
# ---------------------------------------------------------------------------

class AgentState(TypedDict):
    """
    Estado compartilhado do Grafo LangGraph — a 'memória de trabalho'
    do Agente Consultor Financeiro durante uma execução completa do grafo.

    Cada nó recebe este estado completo e retorna um dicionário parcial
    com apenas os campos que modificou. O LangGraph aplica os reducers
    definidos via Annotated e faz o merge automático no estado global.

    Ciclo de vida típico neste grafo:
        [inject_context] → [agent] → [tools] → [agent] → ... → [END]
                              ↑_________↑   (loop controlado por `iterations`)
    """

    # -----------------------------------------------------------------------
    # Histórico contínuo da conversa (Human, AI, Tool messages)
    #
    # IMPORTANTE — por que `add_messages` e não `operator.add`:
    #   - `operator.add` simplesmente concatena listas; nunca atualiza itens.
    #   - `add_messages` (langgraph.graph.message) suporta UPDATE por `id`:
    #     se um nó retornar uma mensagem com o mesmo `id` de uma já existente,
    #     ela é substituída em vez de duplicada. Isso é essencial quando o nó
    #     de ferramenta (tool_node) precisa corrigir uma ToolMessage com erro
    #     sem inflar o histórico com duplicatas.
    #   - Para o caso deste agente, onde tools podem falhar e ser re-tentadas,
    #     `add_messages` é a escolha correta.
    # -----------------------------------------------------------------------
    messages: Annotated[list[BaseMessage], add_messages]

    # -----------------------------------------------------------------------
    # Contexto financeiro pré-carregado pelo backend .NET
    #
    # Injetado pelo nó `inject_context` no início do grafo, antes do primeiro
    # ciclo ReAct. Os nós podem consultá-lo diretamente (ex: para saber o saldo
    # antes de sugerir um aporte) sem precisar chamar `consultar_saldos_contas`.
    # Nós de ação podem atualizar campos específicos após mutações de estado
    # financeiro (ex: após `realizar_aporte_meta`, decrementar o saldo da conta).
    # -----------------------------------------------------------------------
    context_data: ContextData

    # -----------------------------------------------------------------------
    # Contador de iterações — guardrail contra loops infinitos
    #
    # Incrementado pelo nó `agent` a cada ciclo ReAct completo.
    # O nó de roteamento (router) verifica:
    #   if state["iterations"] >= MAX_ITERATIONS → rota para END com aviso
    #   else → rota para `tools` se o agente chamou ferramentas, ou END
    #
    # Inicia em 0; não usa `operator.add` — é sobrescrito (sem reducer),
    # pois o nó retorna o novo valor absoluto: {"iterations": state["iterations"] + 1}
    # -----------------------------------------------------------------------
    iterations: int
