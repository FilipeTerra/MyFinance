"""
test_mercado.py — as tools de mercado deixaram de falar com o yfinance/BCB e
passaram a consumir o backend .NET. O que precisa ser travado aqui é a TRADUÇÃO:
o .NET responde em camelCase, mas o dicionário entregue ao LLM tem que manter as
chaves snake_case pt-BR de sempre, senão prompts e exemplos existentes quebram
silenciosamente — e campos nulos precisam sumir, não virar 0.0.
"""
import pytest

from src.Application.Agents.Tools.api import mercado
from src.Application.Agents.Tools.api.errors import ApiOffline, SessionExpired


class _FakeSession:
    """Stub de ApiSession: registra os paths pedidos e devolve o payload combinado."""

    def __init__(self, payload=None, raises=None):
        self._payload = payload
        self._raises = raises
        self.paths = []

    async def get_json(self, path):
        self.paths.append(path)
        if self._raises:
            raise self._raises
        return self._payload


def _tools(session):
    indicadores, selic = mercado.build(session)
    return indicadores, selic


_INDICADORES_NET = {
    "ticker": "PETR4",
    "precoAtualBrl": 41.52,
    "minima52Semanas": 29.31,
    "maxima52Semanas": 50.69,
    "dividendYield": 9.0,
    "dividendYieldMedio5Anos": None,
    "payout": None,
    "dividaBilhoes": 676.28,
    "pl": 4.44,
    "margemEbitda": 49.83,
    "evEbitda": 4.23,
    "crescimentoReceita": 11.23,
    "fluxoCaixaLivreBilhoes": 85.8,
    "returnOnEquity": 27.81,
    "margemLucro": 24.39,
}

_TAXAS_NET = {
    "selicAnualPct": 14.0,
    "selicMensalPct": 1.0979,
    "ipcaAnualPct": 4.44,
    "ipcaMensalPct": 0.3627,
    "jurosRealAnualPct": 9.1536,
    "cdiAnualPct": 13.9,
    "dataReferenciaSelic": "16/09/2026",
    "dataReferenciaIpca": "01/07/2026",
    "fonte": "Banco Central do Brasil (API SGS em tempo real)",
}


async def test_indicadores_traduz_camel_para_snake_pt_br():
    session = _FakeSession(_INDICADORES_NET)
    consultar, _ = _tools(session)

    r = await consultar.ainvoke({"ticker": "petr4"})

    assert r["ticker"] == "PETR4"
    assert r["preco_atual_brl"] == 41.52
    assert r["minima_52_semanas"] == 29.31
    assert r["maxima_52_semanas"] == 50.69
    assert r["dividend_yield"] == 9.0
    assert r["divida_bilhoes"] == 676.28
    assert r["p_l"] == 4.44
    assert r["margem_ebitda"] == 49.83
    assert r["ev_ebitda"] == 4.23
    assert r["crescimento_receita"] == 11.23
    assert r["fluxo_caixa_livre_bilhoes"] == 85.8
    assert r["return_on_equity"] == 27.81
    assert r["margem_lucro"] == 24.39


async def test_indicadores_omite_campos_nulos_em_vez_de_zerar():
    # O LLM leria "payout: 0.0" como fato e repassaria ao usuário como dado real.
    session = _FakeSession(_INDICADORES_NET)
    consultar, _ = _tools(session)

    r = await consultar.ainvoke({"ticker": "PETR4"})

    assert "payout" not in r
    assert "dividend_yield_medio_5_anos" not in r


async def test_indicadores_usa_rota_do_dotnet_com_ticker_normalizado():
    session = _FakeSession(_INDICADORES_NET)
    consultar, _ = _tools(session)

    await consultar.ainvoke({"ticker": "  petr4 "})

    assert session.paths == ["/mercado/indicadores/PETR4"]


async def test_selic_traduz_os_nove_campos():
    session = _FakeSession(_TAXAS_NET)
    _, buscar_selic = _tools(session)

    r = await buscar_selic.ainvoke({})

    assert r["selic_anual_pct"] == 14.0
    assert r["selic_mensal_pct"] == 1.0979
    assert r["ipca_anual_pct"] == 4.44
    assert r["ipca_mensal_pct"] == 0.3627
    assert r["juros_real_anual_pct"] == 9.1536
    assert r["cdi_anual_pct"] == 13.9
    assert r["data_referencia_selic"] == "16/09/2026"
    assert r["data_referencia_ipca"] == "01/07/2026"
    assert "tempo real" in r["fonte"]
    assert session.paths == ["/mercado/taxas-referencia"]


async def test_tool_selic_preserva_nome_e_marcacao_de_dado_monetario():
    # tool_registry deriva as tools "retorna_dinheiro" da introspecção; perder
    # o nome ou o extras aqui quebraria o contrato do agente silenciosamente.
    _, buscar_selic = _tools(_FakeSession(_TAXAS_NET))

    assert buscar_selic.name == "buscar_taxa_selic"
    assert buscar_selic.extras.get("retorna_dinheiro") is True


@pytest.mark.parametrize("erro,esperado", [
    (ApiOffline("conexão recusada"), "offline"),
    (SessionExpired(), "Sessão expirada"),
])
async def test_erros_de_transporte_viram_dict_de_erro(erro, esperado):
    session = _FakeSession(raises=erro)
    consultar, _ = _tools(session)

    r = await consultar.ainvoke({"ticker": "PETR4"})

    assert "erro" in r
    assert esperado.lower() in r["erro"].lower()
