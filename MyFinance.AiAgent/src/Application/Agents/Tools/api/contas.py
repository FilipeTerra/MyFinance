"""contas.py — Ferramenta de consulta de saldos e contas bancárias."""
import logging

from langchain_core.tools import tool

from .errors import handle_api_errors
from .session import ApiSession
from . import routes

_logger = logging.getLogger("myfinance.agent")


def build(session: ApiSession) -> list:
    @tool
    @handle_api_errors()
    async def consultar_saldos_contas() -> str:
        """Use esta ferramenta para verificar o saldo atual, listar as contas bancárias
        do usuário ou ver quanto dinheiro ele tem disponível. Retorna uma lista de contas e saldos."""
        contas = await session.get_json(routes.ACCOUNTS) or []
        if not contas:
            return "O usuário ainda não possui contas cadastradas."

        # Saída compacta: só o que o LLM precisa (nome, tipo, saldo e o id
        # para ações como realizar_aporte_meta). JSON cru inflaria o contexto.
        linhas = [f"🏦 {len(contas)} conta(s):"]
        total = 0.0
        for c in contas:
            saldo = c.get("currentBalance", 0.0)
            total += saldo
            linhas.append(
                f"  • {c.get('name', 'Sem nome')} ({c.get('typeName', 'Conta')}): "
                f"R$ {saldo:,.2f} | id={c.get('id', '?')}"
            )
        linhas.append(f"  💰 Saldo total: R$ {total:,.2f}")
        _logger.info("🏦 [TOOL:saldos] %d conta(s) | saldo total R$ %.2f", len(contas), total)
        return "\n".join(linhas)

    return [consultar_saldos_contas]
