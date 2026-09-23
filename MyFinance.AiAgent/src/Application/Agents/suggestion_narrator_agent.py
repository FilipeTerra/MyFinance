"""
suggestion_narrator_agent.py — Redator do texto consultivo da sugestão de aporte.

Ponto de entrada: narrate_suggestion(facts: dict) -> dict

Aqui o LLM tem o papel mais estreito de todo o agente: ele NÃO decide nada e NÃO
calcula nada. A API .NET já resolveu, de forma determinística, se o aporte cabe
no orçamento, quanto falta e de onde cortar; este módulo só transforma esses
números em duas a quatro frases de conselho.

Por isso não há grafo, tool-calling nem acesso ao banco — é uma única chamada de
LLM sobre um bloco de fatos, mais uma consulta opcional ao RAG para embasar o
conselho em literatura de finanças pessoais. Se o RAG falhar, o texto sai sem ele.

A validação de que nenhum valor foi inventado acontece do lado .NET
(SugestaoAporteService), que compara cada "R$ x" da resposta com os fatos que
enviou e descarta o texto inteiro se algo não bater.
"""

import asyncio
import logging

from langchain_core.messages import HumanMessage, SystemMessage

from src.Application.Agents.prompts import SUGGESTION_NARRATOR_PROMPT
from src.Infra.Data.financial_rag import get_financial_knowledge_base
from src.Infra.Llm.ollama_provider import get_chat_llm, ainvoke_with_retry

_logger = logging.getLogger("myfinance.agent")

_LLM_TEMPERATURE = 0.2
_LLM_NUM_CTX = 4096
_LLM_TIMEOUT_S = 20.0

_RAG_QUERY_CABE = "pagar-se primeiro, aporte mensal automático, construir patrimônio com constância"
_RAG_QUERY_NAO_CABE = "cortar gastos supérfluos, viver com menos do que se ganha, orçamento apertado"


def _moeda(valor) -> str:
    """Formata no padrão brasileiro, igual ao que a API .NET valida na volta."""
    return f"R$ {float(valor):,.2f}".replace(",", "_").replace(".", ",").replace("_", ".")


def _montar_bloco_de_fatos(facts: dict) -> str:
    """
    Transforma os números já calculados em texto. Tudo que o LLM pode citar precisa
    estar aqui — a API descarta a resposta que mencionar qualquer outro valor.
    """
    cabe = bool(facts.get("cabe"))
    linhas = [
        "=== FATOS JÁ CALCULADOS (use apenas estes valores) ===",
        f"Aporte mensal necessário: {_moeda(facts.get('aporteMensalNecessario', 0))}",
        f"Renda mensal: {_moeda(facts.get('rendaMensal', 0))}",
        f"Despesa mensal média: {_moeda(facts.get('despesaMensalMedia', 0))}",
        f"Sobra mensal livre: {_moeda(facts.get('sobraLivre', 0))}",
        f"O aporte cabe no orçamento: {'sim' if cabe else 'não'}",
    ]

    percentual = facts.get("percentualDaRenda")
    if percentual is not None:
        linhas.append(f"O aporte consome {float(percentual):.1f}% da renda")

    if cabe:
        linhas.append(f"Folga que ainda sobra depois do aporte: {_moeda(facts.get('folgaRestante', 0))}")
    else:
        linhas.append(f"Falta por mês para bancar o aporte: {_moeda(facts.get('deficit', 0))}")

    cortes = facts.get("cortes") or []
    if cortes:
        linhas.append("Cortes sugeridos:")
        for corte in cortes:
            linhas.append(f"  - {corte.get('categoria', '?')}: {_moeda(corte.get('valorCorte', 0))} por mês")

    if (faltante := facts.get("reservaFaltante")) is not None:
        linhas.append(f"Reserva de emergência incompleta — falta {_moeda(faltante)}")

    if (prazo := facts.get("prazoMesesAlternativo")) is not None:
        linhas.append(f"Alternativa: o mesmo alvo é atingido em {int(prazo)} meses com o que ele sustenta hoje")

    if (alvo := facts.get("valorAlvoAlternativo")) is not None:
        linhas.append(f"Alternativa: no prazo original ele alcançaria {_moeda(alvo)}")

    linhas.append("=== FIM DOS FATOS ===")
    return "\n".join(linhas)


async def _buscar_trecho_rag(cabe: bool) -> str:
    """Contexto de literatura financeira. Opcional: falha aqui não impede o texto."""
    try:
        kb = get_financial_knowledge_base()
        query = _RAG_QUERY_CABE if cabe else _RAG_QUERY_NAO_CABE
        return await asyncio.to_thread(kb.search, query, 2)
    except Exception as e:
        _logger.warning("⚠️  [NARRADOR] RAG indisponível, seguindo sem embasamento: %s", e)
        return ""


async def narrate_suggestion(facts: dict) -> dict:
    """
    Redige o texto consultivo da sugestão de aporte.

    Devolve {"success": True, "texto": ...} ou {"success": False, "erro": ...} —
    nunca levanta: sem este texto a API usa o template determinístico dela.
    """
    try:
        bloco = _montar_bloco_de_fatos(facts)
        trecho = await _buscar_trecho_rag(bool(facts.get("cabe")))

        conteudo = bloco
        if trecho:
            conteudo += f"\n\nTrecho de livro de finanças pessoais (parafraseie, não cite a fonte):\n{trecho}"

        llm = get_chat_llm(
            "chat",
            temperature=_LLM_TEMPERATURE,
            num_ctx=_LLM_NUM_CTX,
            timeout=_LLM_TIMEOUT_S,
        )

        resposta = await ainvoke_with_retry(
            llm,
            [SystemMessage(content=SUGGESTION_NARRATOR_PROMPT), HumanMessage(content=conteudo)],
            label="NARRADOR",
        )

        texto = str(resposta.content).strip()
        if not texto:
            return {"success": False, "erro": "O modelo não retornou texto."}

        _logger.info("💬 [NARRADOR] Texto consultivo gerado (%d caracteres).", len(texto))
        return {"success": True, "texto": texto}

    except Exception:
        _logger.exception("❌ [NARRADOR] Falha ao redigir a sugestão de aporte")
        return {"success": False, "erro": "Não foi possível redigir a sugestão."}
