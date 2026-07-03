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

A decisão de EXIBIR (ou não) o card, e QUAL variante mostrar, é feita aqui —
não no frontend — para que exista uma única fonte de verdade:

  reserva_adequada == True
    → não exibe nenhum card (o usuário já tem os 6 meses de renda guardados).

  já iniciou (tem meta "reserva" OU investimento em Renda Fixa) mas ainda não
  atingiu o ideal
    → card tipo "aviso": incentiva a CONTINUAR investindo até completar.
      Não oferece o botão de criar meta (já existe um veículo de poupança).

  não iniciou nada (sem meta de reserva e sem investimento em Renda Fixa)
    → card tipo "info": o card padrão atual, convidando a começar agora,
      com o botão de criação imediata da meta "Reserva de Emergência".

O conteúdo de cada card é dividido em 3 blocos curtos (pedagógico, não um
parágrafo único): curiosidade, informação (diagnóstico numérico) e sugestão
(próximo passo).
"""

import logging

from src.Application.Agents.Tools.api_tools import make_api_tools

_logger = logging.getLogger("myfinance.agent")

_CURIOSIDADE_EM_ANDAMENTO = (
    "Reservas de emergência se constroem aos poucos — o que mais importa "
    "é manter a constância dos aportes, mês após mês."
)
_CURIOSIDADE_NAO_INICIADA = (
    "Imprevistos como perda de emprego ou uma emergência médica não avisam — "
    "por isso ter uma reserva faz toda a diferença."
)


def _montar_informacao_progresso(dados: dict) -> str:
    """Diagnóstico numérico — usado tanto no card de aviso quanto no informativo."""
    return (
        f"Você já guardou {dados['percentual_atingido']:.0f}% da sua reserva ideal. "
        f"Faltam R$ {dados['valor_faltante']:,.2f}."
    )


def _montar_conteudo(dados: dict) -> dict:
    """
    Decide se o card deve ser exibido e monta os 3 blocos curtos
    (curiosidade/informação/sugestão) a partir dos números da ferramenta.
    """
    if dados["reserva_adequada"]:
        return {"exibir_card": False}

    ja_iniciou = dados["possui_meta_reserva"] or dados["possui_investimento_renda_fixa"]

    if ja_iniciou:
        return {
            "exibir_card": True,
            "tipo_card": "aviso",
            "curiosidade": _CURIOSIDADE_EM_ANDAMENTO,
            "informacao": _montar_informacao_progresso(dados),
            "sugestao": "Continue investindo até completar os 6 meses de renda guardados.",
        }

    return {
        "exibir_card": True,
        "tipo_card": "info",
        "curiosidade": _CURIOSIDADE_NAO_INICIADA,
        "informacao": (
            f"Sua reserva ideal é R$ {dados['valor_ideal_reserva']:,.2f} "
            f"(6x sua renda mensal) e você ainda não começou."
        ),
        "sugestao": "Que tal criar essa meta agora e dar o primeiro passo?",
    }


async def invoke_proactive_analysis(jwt_token: str) -> dict:
    """
    Executa a análise de reserva de emergência para o usuário do JWT.

    Retorna um payload estruturado com a decisão de exibição (exibir_card,
    tipo_card) e — quando aplicável — os 3 blocos de texto e os números para
    o frontend montar o card de insight e, se for o caso, o botão de criação
    imediata da meta "Reserva de Emergência".
    """
    api_tools = make_api_tools(jwt_token)
    reserva_tool = next(t for t in api_tools if t.name == "analisar_reserva_emergencia")

    dados: dict = await reserva_tool.ainvoke({})

    if "erro" in dados:
        _logger.warning("🛡️  [PROACTIVE] Análise abortada: %s", dados["erro"])
        return {"success": False, "erro": dados["erro"]}

    conteudo = _montar_conteudo(dados)
    _logger.info(
        "🛡️  [PROACTIVE] ideal=R$ %.2f | atual=R$ %.2f | adequada=%s | exibir=%s | tipo=%s",
        dados["valor_ideal_reserva"], dados["valor_atual_guardado"], dados["reserva_adequada"],
        conteudo["exibir_card"], conteudo.get("tipo_card"),
    )
    return {
        "success": True,
        **conteudo,
        "valor_ideal": dados["valor_ideal_reserva"],
        "valor_atual": dados["valor_atual_guardado"],
        "valor_faltante": dados["valor_faltante"],
        "percentual_atingido": dados["percentual_atingido"],
    }
