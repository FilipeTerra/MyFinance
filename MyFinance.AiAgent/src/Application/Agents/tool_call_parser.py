"""
tool_call_parser.py — Interceptor de Tool Calls Textuais

Modelos pequenos (≤7b) como llama3.2:3b frequentemente falham em emitir
tool_calls no formato estruturado do AIMessage, colocando JSON cru no campo
.content em vez de popular o campo .tool_calls. Resultado: o roteador vê
tool_calls=0, roteia para END, e o JSON chega ao usuário como resposta final.

Este módulo resolve o problema em duas camadas complementares:

  Camada 1 — fix_agent_output (nó do grafo):
    Posição: entre agent_node e _route.
    Ação: detecta JSON de tool_call no .content, converte em AIMessage
    estruturada. O roteador passa a ver .tool_calls corretos e o ciclo
    ReAct prossegue normalmente, como se o modelo tivesse emitido certo.

  Camada 2 — is_leaked_tool_call (utilitário):
    Posição: _extract_final_response em chat_consultant_agent.py.
    Ação: detecta vazamentos residuais (quando a camada 1 não conseguiu
    parsear) e substitui o JSON técnico por mensagem amigável ao usuário.

Topologia do grafo após integração:
  agent_node → fix_agent_output → _route → tools | fallback | END
"""

import json
import logging
import re
import uuid

from langchain_core.messages import AIMessage

from src.Application.Agents.state import AgentState

_logger = logging.getLogger("myfinance.agent")


# ---------------------------------------------------------------------------
# Detector de tool_call textual
#
# Cobre os formatos mais comuns que llama3.2:3b emite:
#   {"type":"function","name":"X","parameters":{...}}   ← formato OpenAI
#   {"name":"X","parameters":{...}}                     ← formato simplificado
#   {"name":"X","args":{...}}                           ← variante alternativa
# ---------------------------------------------------------------------------
_TOOL_CALL_RE = re.compile(
    r'"type"\s*:\s*"function"'                                    # OpenAI-like explícito
    r'|"name"\s*:\s*"[^"]+"\s*,\s*"(?:parameters|args|arguments)"\s*:',  # name + args
    re.IGNORECASE,
)


def is_leaked_tool_call(content: str) -> bool:
    """
    Retorna True se content parece ser um tool_call emitido como texto.
    Usado como detector rápido antes de tentar o parse completo.
    """
    return bool(content and _TOOL_CALL_RE.search(content))


# ---------------------------------------------------------------------------
# Parser de tool_calls textuais
# ---------------------------------------------------------------------------

def _fix_trailing_commas(text: str) -> str:
    """Remove trailing commas antes de } ou ] — erro frequente em modelos pequenos."""
    return re.sub(r',\s*([}\]])', r'\1', text)


def _parse_segment(segment: str) -> dict | None:
    """
    Tenta parsear um único segmento de texto como um tool_call.

    Tenta o JSON bruto primeiro; se falhar por trailing comma, tenta após
    correção. Aceita os campos 'parameters', 'args' ou 'arguments' como
    portadores dos argumentos da ferramenta.

    Returns:
        {name: str, args: dict} ou None se o segmento não for um tool_call válido.
    """
    segment = segment.strip()
    if not segment or '{' not in segment:
        return None

    for candidate in (segment, _fix_trailing_commas(segment)):
        try:
            obj = json.loads(candidate)
        except (json.JSONDecodeError, ValueError):
            continue

        name = obj.get("name")
        if not name or not isinstance(name, str):
            continue

        args = (
            obj.get("parameters")
            or obj.get("args")
            or obj.get("arguments")
            or {}
        )
        return {"name": name, "args": args if isinstance(args, dict) else {}}

    return None


def parse_text_tool_calls(content: str) -> list[dict]:
    """
    Extrai chamadas de ferramentas de uma string de texto livre.

    Divide o conteúdo por ';' ou quebra de linha e tenta parsear cada
    segmento como tool_call. Corrige trailing commas automaticamente.

    Returns:
        Lista de dicts [{name: str, args: dict}].
        Lista vazia se nenhum call válido for encontrado.
    """
    segments = re.split(r';\s*|\n+', content)
    return [c for seg in segments if (c := _parse_segment(seg)) is not None]


# ===========================================================================
# Nó do grafo — Camada 1
# ===========================================================================

def fix_agent_output(state: AgentState) -> dict:
    """
    Nó interceptor: converte tool_calls textuais em AIMessage estruturada.

    Inserido entre agent_node e _route no StateGraph. Garante que o roteador
    receba mensagens no formato correto independente do comportamento do modelo.

    Casos tratados:
      A) last_msg.tool_calls já preenchido → no-op (modelo funcionou certo).
      B) last_msg.content contém JSON de tool_call → reconstrói AIMessage
         com .tool_calls estruturado; _route verá as chamadas e roteará
         para "tools" normalmente.
      C) last_msg.content é resposta final legítima → no-op; flui para END.
      D) content parece tool_call mas parse falha → no-op + aviso de log;
         is_leaked_tool_call() na camada 2 sanitizará antes de enviar ao usuário.

    Mecanismo de atualização in-place:
      O AIMessage reconstruído reutiliza o id da mensagem original.
      O reducer add_messages detecta IDs duplicados e faz UPDATE (não APPEND),
      mantendo o histórico de mensagens limpo e sem duplicatas.

    Returns:
        {} em no-op, ou {"messages": [AIMessage corrigida]} no caso B.
    """
    last_msg = state["messages"][-1]

    # Caso A — modelo emitiu tool_calls corretamente
    if getattr(last_msg, "tool_calls", None):
        return {}

    content = str(last_msg.content or "")

    # Caso C — resposta final legítima sem indício de tool_call
    if not is_leaked_tool_call(content):
        return {}

    # Casos B / D — tenta extrair os tool_calls do texto
    parsed = parse_text_tool_calls(content)

    if not parsed:
        # Caso D: parece tool_call mas não foi possível parsear
        _logger.warning(
            "⚠️  [PARSER] Conteúdo parece tool_call mas parse falhou"
            " — camada 2 sanitizará: %.120s",
            content,
        )
        return {}

    # Caso B: reconstrói AIMessage com tool_calls estruturados
    structured = [
        {
            "id": f"call_{uuid.uuid4().hex[:12]}",
            "name": c["name"],
            "args": c["args"],
            "type": "tool_call",
        }
        for c in parsed
    ]

    fixed = AIMessage(
        content="",
        tool_calls=structured,
        id=getattr(last_msg, "id", None),
    )

    _logger.info(
        "🔧 [PARSER] %d tool_call(s) textuais convertidos → %s",
        len(structured),
        [c["name"] for c in structured],
    )

    return {"messages": [fixed]}
