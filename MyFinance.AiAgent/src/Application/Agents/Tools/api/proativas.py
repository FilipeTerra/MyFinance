"""
proativas.py — Ferramentas de análise proativa (reserva de emergência e
inflação de estilo de vida).

Expostas tanto ao chat ReAct (via registry.make_api_tools) quanto consumidas
diretamente por proactive_analyzer_agent.py e lifestyle_monitor_agent.py.
"""
import asyncio
import logging
from datetime import datetime, timedelta, timezone

from langchain_core.tools import tool

from .enums import TipoInvestimento
from .errors import handle_api_errors, SessionExpired
from .fluxo import classificar_fluxo
from .session import ApiSession
from . import routes

_logger = logging.getLogger("myfinance.agent")

# Palavras-chave usadas para identificar categorias/descrições de "estilo de
# vida" (gastos supérfluos) — mesma técnica de match usada em
# relatorio_mensal_por_categoria (substring case-insensitive em categoria + descrição).
_KEYWORDS_ESTILO_DE_VIDA = [
    "lazer", "restaurante", "assinatura", "role", "bar", "streaming", "delivery",
]


def build(session: ApiSession) -> list:
    @tool
    @handle_api_errors(as_dict=True)
    async def analisar_reserva_emergencia() -> dict:
        """Use esta ferramenta para diagnosticar se o usuário possui uma reserva de
        emergência adequada. Ela busca a renda mensal do perfil, soma o valor guardado
        em metas financeiras com 'reserva' no nome e em investimentos de Renda Fixa,
        e calcula se esse total atinge o ideal de 6x a renda mensal. Retorna os números
        prontos — NÃO calcule por conta própria, apenas use este resultado."""
        r_perfil, r_metas, r_investimentos = await asyncio.gather(
            session.get_raw(routes.PROFILE), session.get_raw(routes.FINANCIAL_GOALS), session.get_raw(routes.INVESTMENTS),
        )

        if 401 in (r_perfil.status_code, r_metas.status_code, r_investimentos.status_code):
            raise SessionExpired()

        perfil = r_perfil.json() if r_perfil.status_code == 200 else {}
        renda_mensal = (perfil or {}).get("monthlyIncome") or 0.0

        if not renda_mensal:
            return {
                "erro": (
                    "O usuário não possui renda mensal cadastrada no perfil. "
                    "Não é possível calcular a reserva ideal sem essa informação."
                )
            }

        metas = r_metas.json() if r_metas.status_code == 200 else []
        metas_reserva = [
            m for m in (metas or [])
            if "reserva" in (m.get("name") or "").lower()
        ]
        valor_em_metas_reserva = sum(m.get("currentAmount", 0.0) for m in metas_reserva)
        possui_meta_reserva = len(metas_reserva) > 0

        investimentos = r_investimentos.json() if r_investimentos.status_code == 200 else []
        investimentos_renda_fixa = [
            i for i in (investimentos or []) if i.get("tipo") == TipoInvestimento.RENDA_FIXA
        ]
        valor_em_renda_fixa = sum(i.get("valorAtual", 0.0) for i in investimentos_renda_fixa)
        possui_investimento_renda_fixa = len(investimentos_renda_fixa) > 0

        valor_ideal = round(renda_mensal * 6, 2)
        valor_atual = round(valor_em_metas_reserva + valor_em_renda_fixa, 2)
        percentual_atingido = round(valor_atual / valor_ideal * 100, 1) if valor_ideal > 0 else 0.0
        meses_cobertos = round(valor_atual / renda_mensal, 1) if renda_mensal > 0 else 0.0
        reserva_adequada = valor_atual >= valor_ideal
        valor_faltante = round(max(valor_ideal - valor_atual, 0.0), 2)

        _logger.info(
            "🛡️  [TOOL:reserva] renda=R$ %.2f | ideal=R$ %.2f | atual=R$ %.2f | adequada=%s",
            renda_mensal, valor_ideal, valor_atual, reserva_adequada,
        )
        return {
            "renda_mensal": round(renda_mensal, 2),
            "valor_ideal_reserva": valor_ideal,
            "valor_atual_guardado": valor_atual,
            "detalhamento": {
                "em_metas_reserva": round(valor_em_metas_reserva, 2),
                "em_investimentos_renda_fixa": round(valor_em_renda_fixa, 2),
            },
            "meses_de_despesa_cobertos": meses_cobertos,
            "percentual_atingido": percentual_atingido,
            "reserva_adequada": reserva_adequada,
            "valor_faltante": valor_faltante,
            "possui_meta_reserva": possui_meta_reserva,
            "possui_investimento_renda_fixa": possui_investimento_renda_fixa,
        }

    @tool
    @handle_api_errors(as_dict=True)
    async def analisar_inflacao_estilo_vida() -> dict:
        """Use esta ferramenta para diagnosticar 'inflação de estilo de vida': gastos
        supérfluos (lazer, restaurantes, assinaturas, delivery) crescendo no mesmo ritmo
        ou mais rápido que a renda, sem um aumento correspondente nos investimentos.
        Analisa os últimos 6 meses de transações comparando o trimestre mais recente
        com o anterior. Retorna os números prontos — NÃO calcule por conta própria."""
        agora = datetime.now(timezone.utc)
        dt_inicio_6m = agora - timedelta(days=180)
        dt_corte_3m = agora - timedelta(days=90)

        r_perfil, transacoes = await asyncio.gather(
            session.get_raw(routes.PROFILE),
            session.buscar_todas_transacoes(dt_inicio_6m, agora),
        )

        if isinstance(transacoes, str):
            return {"erro": transacoes}

        renda_cadastrada = (r_perfil.json() if r_perfil.status_code == 200 else {}).get(
            "monthlyIncome"
        ) or 0.0

        def _e_estilo_de_vida(t: dict) -> bool:
            texto = f"{t.get('categoryName') or t.get('category') or ''} {t.get('description') or ''}".lower()
            return any(kw in texto for kw in _KEYWORDS_ESTILO_DE_VIDA)

        def _parse_data(t: dict) -> datetime:
            try:
                d = datetime.fromisoformat((t.get("date") or "").replace("Z", "+00:00"))
                return d if d.tzinfo else d.replace(tzinfo=timezone.utc)
            except Exception:
                return agora

        recentes = [t for t in transacoes if _parse_data(t) >= dt_corte_3m]
        anteriores = [t for t in transacoes if _parse_data(t) < dt_corte_3m]

        def _resumo_periodo(grupo: list) -> dict:
            receitas, _despesas, aportes = classificar_fluxo(grupo)
            estilo_vida = sum(
                abs(t.get("amount", 0))
                for t in grupo
                if (t.get("type") == 2 or t.get("amount", 0) < 0) and _e_estilo_de_vida(t)
            )
            return {"receitas": receitas, "aportes": aportes, "estilo_vida": estilo_vida}

        atual = _resumo_periodo(recentes)
        anterior = _resumo_periodo(anteriores)

        def _variacao_pct(novo: float, antigo: float) -> float | None:
            if antigo <= 0:
                return None if novo <= 0 else 100.0
            return round((novo - antigo) / antigo * 100, 1)

        variacao_renda = _variacao_pct(atual["receitas"], anterior["receitas"])
        variacao_estilo_vida = _variacao_pct(atual["estilo_vida"], anterior["estilo_vida"])
        variacao_aportes = _variacao_pct(atual["aportes"], anterior["aportes"])

        media_mensal_estilo_vida = round(atual["estilo_vida"] / 3, 2)
        percentual_da_renda = (
            round(media_mensal_estilo_vida / renda_cadastrada * 100, 1)
            if renda_cadastrada > 0
            else None
        )

        dados_suficientes = len(anteriores) > 0

        if dados_suficientes:
            alerta = bool(
                (variacao_estilo_vida is not None and variacao_aportes is not None
                 and variacao_estilo_vida > variacao_aportes)
                or (variacao_renda is not None and variacao_renda > 5 and (variacao_aportes or 0) <= 0)
                or (percentual_da_renda is not None and percentual_da_renda > 30)
            )
        else:
            alerta = bool(percentual_da_renda is not None and percentual_da_renda > 30)

        _logger.info(
            "📈 [TOOL:inflacao] estilo_vida=R$ %.2f/mês (%.1f%% renda) | var_estilo=%s | var_aportes=%s | alerta=%s",
            media_mensal_estilo_vida, percentual_da_renda or 0.0,
            variacao_estilo_vida, variacao_aportes, alerta,
        )
        return {
            "renda_mensal_cadastrada": round(renda_cadastrada, 2),
            "gasto_estilo_vida_ultimo_trimestre": round(atual["estilo_vida"], 2),
            "gasto_estilo_vida_trimestre_anterior": round(anterior["estilo_vida"], 2),
            "media_mensal_estilo_vida": media_mensal_estilo_vida,
            "percentual_da_renda_em_estilo_vida": percentual_da_renda,
            "variacao_renda_pct": variacao_renda,
            "variacao_estilo_vida_pct": variacao_estilo_vida,
            "variacao_aportes_pct": variacao_aportes,
            "dados_suficientes": dados_suficientes,
            "alerta_inflacao_estilo_vida": alerta,
        }

    return [analisar_reserva_emergencia, analisar_inflacao_estilo_vida]
