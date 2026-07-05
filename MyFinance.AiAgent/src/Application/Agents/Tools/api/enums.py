"""
enums.py — Enums que espelham contratos do backend .NET.

O .NET não tem JsonStringEnumConverter configurado nesses campos, então os
valores chegam como inteiros crus no JSON — os IntEnum abaixo dão nome a esses
números sem mudar o formato de comparação (IntEnum é comparável/hasheável
como int nativo, então `dados.get("tipo") == TipoInvestimento.RENDA_FIXA`
funciona igual a `== 1`).
"""
from enum import IntEnum


class TipoInvestimento(IntEnum):
    """Espelha Domain.Enums.InvestmentType do .NET."""
    RENDA_FIXA = 1
    ACAO = 2
    FII = 3
    CRIPTO = 4
    ETF = 5


TIPO_INVESTIMENTO_LABEL = {
    TipoInvestimento.RENDA_FIXA: "Renda Fixa",
    TipoInvestimento.ACAO: "Ação",
    TipoInvestimento.FII: "FII",
    TipoInvestimento.CRIPTO: "Cripto",
    TipoInvestimento.ETF: "ETF",
}
