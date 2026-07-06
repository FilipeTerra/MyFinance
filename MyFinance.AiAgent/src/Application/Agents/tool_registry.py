"""
tool_registry.py — Deriva metadados estáticos a partir das próprias tools.

Antes, chat_consultant_agent.py mantinha `_DATA_TOOLS`, uma lista de nomes
de ferramentas "que retornam dinheiro" — mantida à mão, num arquivo diferente
de onde as tools são definidas. Toda tool nova precisava ser lembrada e
adicionada manualmente ali; esquecer era um bug silencioso.

Agora cada tool se autodeclara na própria definição via
`@tool(extras={"retorna_dinheiro": True})`, e este módulo só agrega o que
já está declarado — não há uma segunda fonte de verdade para manter em sincronia.
"""
from functools import lru_cache

from src.Application.Agents.nodes import collect_all_tools

# JWT-placeholder usado só para montar os objetos de tool e ler `.extras`.
# ApiSession() não faz nenhuma chamada de rede na construção (só monta um
# httpx.AsyncClient), então isto é seguro e não depende de um JWT real.
_PLACEHOLDER_JWT = "tool-registry-metadata-only"


@lru_cache
def get_data_tool_names() -> frozenset[str]:
    """
    Nomes das tools marcadas com extras={"retorna_dinheiro": True}.

    Usado pela recuperação anti-leak (Camada 2, em chat_consultant_agent.py)
    para saber quais outputs de ferramenta do turno atual contêm valores
    monetários recuperáveis caso a resposta do LLM esteja corrompida.

    Resultado cacheado: a lista de tools e seus metadados são estáticos
    (não dependem do JWT), então a introspecção roda uma única vez por processo.
    """
    all_tools = collect_all_tools(_PLACEHOLDER_JWT)
    return frozenset(
        t.name for t in all_tools
        if t.extras and t.extras.get("retorna_dinheiro")
    )
