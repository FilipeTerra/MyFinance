"""
test_tool_registry.py — trava o conjunto de tools marcadas com
extras={"retorna_dinheiro": True}. Se alguém remover a tag de uma tool (ou
esquecer de marcar uma nova), este teste é quem denuncia — não a memória de
quem revisar o PR.
"""
from src.Application.Agents.tool_registry import get_data_tool_names

_ESPERADAS = frozenset({
    "calcular_resumo_financeiro",
    "analisar_gastos_por_categoria",
    "simular_investimento",
    "consultar_metas_financeiras",
    "consultar_saldos_contas",
    "consultar_transacoes_recentes",
    "buscar_taxa_selic",
    "relatorio_mensal_por_categoria",
})


def test_get_data_tool_names_retorna_exatamente_as_tools_marcadas():
    assert get_data_tool_names() == _ESPERADAS


def test_get_data_tool_names_e_cacheado():
    assert get_data_tool_names() is get_data_tool_names()
