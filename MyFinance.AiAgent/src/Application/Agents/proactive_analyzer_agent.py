"""
proactive_analyzer_agent.py — Agente Proativo de Análise de Reserva de Emergência

Ponto de entrada: invoke_proactive_analysis(jwt_token) -> dict

Diferente do chat_consultant_agent (conversa livre, precisa do LLM para
interpretar intenção), este insight é um banner de dashboard: precisa de
texto curto, direto e SEMPRE consistente, além dos números exatos para o
frontend montar o botão "criar meta". Por isso o conteúdo é montado por
template determinístico em cima do resultado 100% calculado pela ferramenta
analisar_reserva_emergencia — sem round-trip ao Ollama nesta tela, sem
variação de tom ou tamanho entre chamadas.

O conteúdo é dividido em 3 blocos curtos (pedagógico, não um parágrafo único):
  curiosidade — fato educativo, fixo, sobre reserva de emergência;
  informacao  — diagnóstico numérico personalizado do usuário;
  sugestao    — próximo passo recomendado, alinhado ao botão de ação do card.
"""

import logging

from src.Application.Agents.Tools.api_tools import make_api_tools

_logger = logging.getLogger("myfinance.agent")

_CURIOSIDADE_RESERVA_OK = (
    "Ter uma reserva de emergência é o que especialistas chamam de "
    "'colchão financeiro' — sua proteção contra imprevistos."
)
_CURIOSIDADE_RESERVA_ZERADA = (
    "Imprevistos como perda de emprego ou uma emergência médica não avisam — "
    "por isso ter uma reserva faz toda a diferença."
)
_CURIOSIDADE_RESERVA_PARCIAL = (
    "Especialistas recomendam guardar de 3 a 6 meses de despesas para imprevistos "
    "— é a chamada reserva de emergência."
)


def _montar_conteudo(dados: dict) -> dict:
    """Monta os 3 blocos curtos (curiosidade/informação/sugestão) a partir dos números da ferramenta."""
    valor_atual = dados["valor_atual_guardado"]
    valor_ideal = dados["valor_ideal_reserva"]
    valor_faltante = dados["valor_faltante"]
    percentual = dados["percentual_atingido"]
    meses = dados["meses_de_despesa_cobertos"]
    possui_meta = dados["possui_meta_reserva"]

    if dados["reserva_adequada"]:
        return {
            "curiosidade": _CURIOSIDADE_RESERVA_OK,
            "informacao": f"Você tem R$ {valor_atual:,.2f} guardados, cobrindo {meses:.0f} meses de renda.",
            "sugestao": "Reserva completa! Direcione o que sobrar para investimentos de longo prazo. 🎉",
        }

    if valor_atual <= 0:
        return {
            "curiosidade": _CURIOSIDADE_RESERVA_ZERADA,
            "informacao": f"Sua reserva ideal é R$ {valor_ideal:,.2f} (6x sua renda mensal) e você ainda não começou.",
            "sugestao": "Que tal criar essa meta agora e dar o primeiro passo?",
        }

    sugestao = (
        "Continue os aportes na sua meta de reserva até completá-la."
        if possui_meta
        else "Separe uma parte fixa do seu salário todo mês até completar a meta."
    )
    return {
        "curiosidade": _CURIOSIDADE_RESERVA_PARCIAL,
        "informacao": f"Você já guardou {percentual:.0f}% da sua reserva ideal. Faltam R$ {valor_faltante:,.2f}.",
        "sugestao": sugestao,
    }


async def invoke_proactive_analysis(jwt_token: str) -> dict:
    """
    Executa a análise de reserva de emergência para o usuário do JWT.

    Retorna um payload estruturado — números + os 3 blocos de texto (curiosidade,
    informação, sugestão) — pronto para o frontend montar o card de insight e,
    se aplicável, o botão de criação imediata da meta "Reserva de Emergência".
    """
    api_tools = make_api_tools(jwt_token)
    reserva_tool = next(t for t in api_tools if t.name == "analisar_reserva_emergencia")

    dados: dict = await reserva_tool.ainvoke({})

    if "erro" in dados:
        _logger.warning("🛡️  [PROACTIVE] Análise abortada: %s", dados["erro"])
        return {"success": False, "erro": dados["erro"]}

    _logger.info(
        "🛡️  [PROACTIVE] ideal=R$ %.2f | atual=R$ %.2f | adequada=%s",
        dados["valor_ideal_reserva"], dados["valor_atual_guardado"], dados["reserva_adequada"],
    )
    return {
        "success": True,
        **_montar_conteudo(dados),
        "reserva_adequada": dados["reserva_adequada"],
        "possui_meta_reserva": dados["possui_meta_reserva"],
        "valor_ideal": dados["valor_ideal_reserva"],
        "valor_atual": dados["valor_atual_guardado"],
        "valor_faltante": dados["valor_faltante"],
        "percentual_atingido": dados["percentual_atingido"],
    }
