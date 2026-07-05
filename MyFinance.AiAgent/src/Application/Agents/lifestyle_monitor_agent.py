"""
lifestyle_monitor_agent.py — Agente Proativo de Monitor de Inflação do Estilo de Vida

Ponto de entrada: invoke_lifestyle_monitor(jwt_token) -> dict

Detecta "inflação de estilo de vida": gastos supérfluos (lazer, restaurantes,
assinaturas) crescendo no mesmo ritmo — ou mais rápido — que a renda, sem um
aumento correspondente nos investimentos.

Fluxo (LangGraph, 3 nós lineares, sem loop de tool-calling):
  calcular ──► [sem alerta] ──► sem_alerta  (determinístico, sem custo de LLM)
            └► [com alerta] ──► com_alerta  (RAG + 1 chamada ao LLM)

Por que ramificar em vez de sempre chamar o LLM: quando não há nada a avisar,
gerar texto por LLM só adicionaria latência e risco de variação sem necessidade
— o caso feliz (sem alerta) é 100% determinístico. O LLM só entra em cena para
a tarefa que só ele resolve bem: parafrasear um trecho de livro (RAG) numa
frase curta e conectá-lo à sugestão de ação.

Segue o mesmo formato de card das demais análises proativas: 3 blocos curtos
(curiosidade / informação / sugestão) — números sempre calculados em Python,
nunca pelo LLM.
"""

import asyncio
import logging
import re

from langchain_core.messages import HumanMessage, SystemMessage
from langgraph.graph import END, START, StateGraph
from typing import TypedDict

from src.Application.Agents.Tools.api_tools import make_api_tools
from src.Infra.Data.financial_rag import FinancialKnowledgeBase
from src.Infra.Llm.ollama_provider import get_chat_llm, ainvoke_with_retry

_logger = logging.getLogger("myfinance.agent")

# Singleton — mesmo índice FAISS reutilizado por consultar_teoria_financeira
_kb = FinancialKnowledgeBase()

_LLM_TEMPERATURE = 0.2
_LLM_NUM_CTX = 4096
_LLM_TIMEOUT_S = 60.0

_RAG_QUERY = (
    "inflação de estilo de vida, aumentar o padrão de vida junto com a renda, "
    "gastos supérfluos, custo de vida fixo"
)

_CURIOSIDADE_FALLBACK = (
    "Aumentar o padrão de vida na mesma velocidade da renda é chamado de "
    "'inflação de estilo de vida' — e pode travar a construção de patrimônio."
)
_SUGESTAO_FALLBACK_ALERTA = (
    "Direcione parte do aumento da sua renda para investimentos antes de aumentar os gastos."
)
_CURIOSIDADE_SEM_ALERTA = (
    "Manter os gastos supérfluos estáveis enquanto a renda cresce é o segredo "
    "para acumular patrimônio mais rápido."
)
_SUGESTAO_SEM_ALERTA = "Continue assim! Seus investimentos estão acompanhando bem seus gastos."

_SYSTEM_PROMPT = (
    "Você é o Claudio, assistente financeiro. O usuário está com sinais de 'inflação de "
    "estilo de vida' (gastos supérfluos crescendo no mesmo ritmo ou mais rápido que a renda, "
    "sem aumento correspondente nos investimentos).\n\n"
    "Você recebeu um trecho de um livro de educação financeira sobre o tema. Responda "
    "SOMENTE neste formato, sem nada além disso:\n\n"
    "CURIOSIDADE: <1 frase curta parafraseando o ensinamento do trecho, em português, "
    "sem citar nome de livro ou autor>\n"
    "SUGESTAO: <1 frase curta e prática de ação recomendada para o usuário>\n\n"
    "Regras: cada linha deve ter no máximo ~20 palavras; não use markdown, aspas ou listas; "
    "não invente números — os números já foram calculados e serão exibidos separadamente."
)


class LifestyleState(TypedDict, total=False):
    dados: dict
    resultado: dict


def _montar_informacao(dados: dict) -> str:
    """Diagnóstico numérico — sempre determinístico, nunca gerado pelo LLM."""
    pct_renda = dados.get("percentual_da_renda_em_estilo_vida")
    var_estilo = dados.get("variacao_estilo_vida_pct")
    var_aportes = dados.get("variacao_aportes_pct")

    if dados.get("dados_suficientes") and var_estilo is not None and var_aportes is not None:
        return (
            f"Nos últimos 3 meses, seus gastos com lazer/assinaturas variaram {var_estilo:+.0f}%, "
            f"enquanto seus aportes em investimentos variaram {var_aportes:+.0f}%."
        )
    if pct_renda is not None:
        return (
            f"Seus gastos com lazer, restaurantes e assinaturas somam "
            f"R$ {dados['media_mensal_estilo_vida']:,.2f} por mês ({pct_renda:.0f}% da sua renda)."
        )
    return (
        f"Seus gastos com lazer, restaurantes e assinaturas somam "
        f"R$ {dados['media_mensal_estilo_vida']:,.2f} por mês."
    )


def _parse_resposta_llm(texto: str) -> tuple[str | None, str | None]:
    m_cur = re.search(r"CURIOSIDADE:\s*(.+)", texto, re.IGNORECASE)
    m_sug = re.search(r"SUGEST[AÃ]O:\s*(.+)", texto, re.IGNORECASE)
    curiosidade = m_cur.group(1).strip() if m_cur else None
    sugestao = m_sug.group(1).strip() if m_sug else None
    return curiosidade, sugestao


def _numeros_publicos(dados: dict) -> dict:
    return {
        "percentual_renda_estilo_vida": dados.get("percentual_da_renda_em_estilo_vida"),
        "variacao_estilo_vida_pct": dados.get("variacao_estilo_vida_pct"),
        "variacao_aportes_pct": dados.get("variacao_aportes_pct"),
    }


async def _gerar_conteudo_com_rag(dados: dict) -> dict:
    informacao = _montar_informacao(dados)
    snippet = await asyncio.to_thread(_kb.search, _RAG_QUERY, 2)

    llm = get_chat_llm(
        "chat",
        temperature=_LLM_TEMPERATURE,
        num_ctx=_LLM_NUM_CTX,
        timeout=_LLM_TIMEOUT_S,
    )

    mensagens = [
        SystemMessage(content=_SYSTEM_PROMPT),
        HumanMessage(
            content=(
                f"Diagnóstico do usuário: {informacao}\n\n"
                f"Trecho de livro de finanças pessoais sobre o tema:\n{snippet}"
            )
        ),
    ]
    resposta = await ainvoke_with_retry(llm, mensagens, label="LIFESTYLE")

    curiosidade, sugestao = _parse_resposta_llm(str(resposta.content))
    return {
        "curiosidade": curiosidade or _CURIOSIDADE_FALLBACK,
        "informacao": informacao,
        "sugestao": sugestao or _SUGESTAO_FALLBACK_ALERTA,
    }


def _build_graph(jwt_token: str):
    api_tools = make_api_tools(jwt_token)
    inflacao_tool = next(t for t in api_tools if t.name == "analisar_inflacao_estilo_vida")

    async def calcular_node(state: LifestyleState) -> dict:
        dados: dict = await inflacao_tool.ainvoke({})
        return {"dados": dados}

    def _route(state: LifestyleState) -> str:
        dados = state["dados"]
        if "erro" in dados or not dados.get("alerta_inflacao_estilo_vida"):
            return "sem_alerta"
        return "com_alerta"

    async def sem_alerta_node(state: LifestyleState) -> dict:
        dados = state["dados"]
        if "erro" in dados:
            return {"resultado": {"success": False, "erro": dados["erro"]}}
        return {
            "resultado": {
                "success": True,
                "alerta": False,
                "curiosidade": _CURIOSIDADE_SEM_ALERTA,
                "informacao": _montar_informacao(dados),
                "sugestao": _SUGESTAO_SEM_ALERTA,
                **_numeros_publicos(dados),
            }
        }

    async def com_alerta_node(state: LifestyleState) -> dict:
        dados = state["dados"]
        conteudo = await _gerar_conteudo_com_rag(dados)
        return {
            "resultado": {
                "success": True,
                "alerta": True,
                **conteudo,
                **_numeros_publicos(dados),
            }
        }

    workflow = StateGraph(LifestyleState)
    workflow.add_node("calcular", calcular_node)
    workflow.add_node("sem_alerta", sem_alerta_node)
    workflow.add_node("com_alerta", com_alerta_node)
    workflow.add_edge(START, "calcular")
    workflow.add_conditional_edges("calcular", _route, {"sem_alerta": "sem_alerta", "com_alerta": "com_alerta"})
    workflow.add_edge("sem_alerta", END)
    workflow.add_edge("com_alerta", END)

    return workflow.compile()


async def invoke_lifestyle_monitor(jwt_token: str) -> dict:
    """
    Executa o Monitor de Inflação do Estilo de Vida para o usuário do JWT.

    Retorna um payload estruturado (números + os 3 blocos de texto) pronto
    para o frontend renderizar o card de insight.
    """
    graph = _build_graph(jwt_token)
    result: dict = await graph.ainvoke({})

    resultado = result.get("resultado") or {
        "success": False,
        "erro": "Não foi possível gerar a análise agora.",
    }
    _logger.info(
        "📈 [LIFESTYLE] success=%s | alerta=%s",
        resultado.get("success"), resultado.get("alerta"),
    )
    return resultado
