"""
registry.py — Ponto de composição das ferramentas de API autenticadas.

Substitui o antigo api_tools.py monolítico (1211 linhas, 14 tools numa única
função). Cada domínio (contas, metas, transações, perfil, proativas) vive em
seu próprio módulo e expõe build(session) -> list[BaseTool]; este arquivo só
cria a ApiSession compartilhada e concatena o resultado de cada um.
"""
from . import contas, metas, perfil, proativas, transacoes
from .session import ApiSession


def make_api_tools(jwt_token: str) -> list:
    """
    Factory que cria as ferramentas HTTP com o JWT baked-in via closure.
    O LLM nunca vê o token — ele apenas chama as ferramentas pelo nome.

    Uma única ApiSession (AsyncClient autenticado) é criada por chamada e
    compartilhada por todas as tools da mesma requisição (headers e pool de
    conexão reutilizados). Finalizada pelo GC quando o grafo encerra.
    """
    session = ApiSession(jwt_token)
    return [
        *contas.build(session),
        *metas.build(session),
        *transacoes.build(session),
        *perfil.build(session),
        *proativas.build(session),
    ]
