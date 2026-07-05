"""perfil.py — Ferramentas de consulta de perfil e investimentos do usuário."""
import logging

from langchain_core.tools import tool

from .enums import TIPO_INVESTIMENTO_LABEL
from .errors import handle_api_errors
from .session import ApiSession
from . import routes

_logger = logging.getLogger("myfinance.agent")


def build(session: ApiSession) -> list:
    @tool
    @handle_api_errors()
    async def consultar_perfil_usuario() -> str:
        """Use esta ferramenta para consultar os dados de perfil do usuário logado,
        incluindo nome, e-mail e a renda mensal (salário) cadastrada. Necessária para
        qualquer cálculo que dependa da renda do usuário (ex: reserva de emergência)."""
        perfil = await session.get_json(routes.PROFILE) or {}
        renda = perfil.get("monthlyIncome")
        linhas = [f"👤 Perfil de {perfil.get('name', 'usuário')}:"]
        if renda is not None:
            linhas.append(f"  • Renda mensal cadastrada: R$ {renda:,.2f}")
        else:
            linhas.append("  • Renda mensal não cadastrada.")
        _logger.info("👤 [TOOL:perfil] renda=%s", renda)
        return "\n".join(linhas)

    @tool
    @handle_api_errors()
    async def consultar_investimentos() -> str:
        """Use esta ferramenta para listar os investimentos do usuário (Renda Fixa,
        Ação, FII, Cripto, ETF), com valor investido, valor atual e rentabilidade."""
        investimentos = await session.get_json(routes.INVESTMENTS) or []
        if not investimentos:
            return "O usuário ainda não possui investimentos cadastrados."

        linhas = [f"📈 {len(investimentos)} investimento(s):"]
        for i in investimentos:
            tipo = TIPO_INVESTIMENTO_LABEL.get(i.get("tipo"), str(i.get("tipo", "?")))
            linhas.append(
                f"  • {i.get('nome', '?')} ({tipo}): R$ {i.get('valorAtual', 0.0):,.2f} "
                f"(investido R$ {i.get('valorInicial', 0.0):,.2f}, "
                f"rentabilidade {i.get('rentabilidadePercentual', 0.0):.1f}%)"
            )
        _logger.info("📈 [TOOL:investimentos] %d investimento(s) retornados", len(investimentos))
        return "\n".join(linhas)

    return [consultar_perfil_usuario, consultar_investimentos]
