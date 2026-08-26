"""
mercado.py — Ferramentas de dados de mercado (B3 e taxas de referência).

As integrações externas (brapi/B3, Banco Central) vivem na camada Infrastructure
do backend .NET, não aqui. Este módulo só traduz a resposta do .NET para o
vocabulário que o agente já usa — o AiAgent cuida de IA, o .NET cuida de dados.

Contrato de tradução: o .NET responde em camelCase, mas o dicionário devolvido ao
LLM mantém as chaves em snake_case pt-BR exatamente como antes, para que prompts
e exemplos existentes continuem valendo.

Campos que o provedor não disponibiliza vêm como null e são OMITIDOS do resultado
— nunca convertidos para 0.0, que o modelo leria como fato ("payout de 0%").
"""
from langchain_core.tools import tool
from pydantic import BaseModel, Field

from . import routes
from .errors import handle_api_errors
from .session import ApiSession


class ConsultarAcaoB3Input(BaseModel):
    ticker: str = Field(
        ...,
        description=(
            "O ticker da ação na B3 que o usuário deseja analisar (ex: PETR4, VALE3, WEGE3). "
            "Sempre passe apenas o código de 4 a 6 caracteres."
        )
    )


# Mapeamento camelCase (.NET) → snake_case pt-BR (vocabulário do agente).
_INDICADORES_MAP = {
    "precoAtualBrl": "preco_atual_brl",
    "minima52Semanas": "minima_52_semanas",
    "maxima52Semanas": "maxima_52_semanas",
    "dividendYield": "dividend_yield",
    "dividendYieldMedio5Anos": "dividend_yield_medio_5_anos",
    "payout": "payout",
    "dividaBilhoes": "divida_bilhoes",
    "pl": "p_l",
    "margemEbitda": "margem_ebitda",
    "evEbitda": "ev_ebitda",
    "crescimentoReceita": "crescimento_receita",
    "fluxoCaixaLivreBilhoes": "fluxo_caixa_livre_bilhoes",
    "returnOnEquity": "return_on_equity",
    "margemLucro": "margem_lucro",
}

_TAXAS_MAP = {
    "selicAnualPct": "selic_anual_pct",
    "selicMensalPct": "selic_mensal_pct",
    "ipcaAnualPct": "ipca_anual_pct",
    "ipcaMensalPct": "ipca_mensal_pct",
    "jurosRealAnualPct": "juros_real_anual_pct",
    "cdiAnualPct": "cdi_anual_pct",
    "dataReferenciaSelic": "data_referencia_selic",
    "dataReferenciaIpca": "data_referencia_ipca",
    "fonte": "fonte",
}


def _traduzir(dados: dict, mapa: dict) -> dict:
    """Converte as chaves do .NET para o vocabulário do agente, omitindo nulos."""
    return {
        destino: dados[origem]
        for origem, destino in mapa.items()
        if dados.get(origem) is not None
    }


def build(session: ApiSession) -> list:
    """Cria as ferramentas de mercado ligadas à sessão HTTP autenticada."""

    @tool(args_schema=ConsultarAcaoB3Input)
    @handle_api_errors(as_dict=True)
    async def consultar_indicadores_b3(ticker: str) -> dict:
        """
        Busca indicadores fundamentalistas e preço atual de uma empresa da B3.
        Use OBRIGATORIAMENTE quando o usuário perguntar sobre
        ações específicas, valuation ou se vale a pena investir em uma empresa.

        Nem todo indicador está disponível para todo ativo: os que vierem ausentes
        do resultado simplesmente não existem para aquela empresa — não os comente
        nem assuma que valem zero.
        """
        path = routes.MERCADO_INDICADORES.format(ticker=ticker.upper().strip())
        dados = await session.get_json(path)
        resultado = {"ticker": dados.get("ticker", ticker.upper().strip())}
        resultado.update(_traduzir(dados, _INDICADORES_MAP))
        return resultado

    @tool(extras={"retorna_dinheiro": True})
    @handle_api_errors(as_dict=True)
    async def buscar_taxa_selic() -> dict:
        """
        Retorna as taxas de referência da economia brasileira: SELIC (taxa básica de juros)
        e IPCA (inflação oficial), bem como os juros reais (SELIC descontada a inflação).

        Use esta ferramenta ANTES de qualquer simulação de investimento ou análise de
        cenário econômico que dependa de juros ou inflação.
        """
        dados = await session.get_json(routes.MERCADO_TAXAS_REFERENCIA)
        return _traduzir(dados, _TAXAS_MAP)

    return [consultar_indicadores_b3, buscar_taxa_selic]
