"""
investment_tools.py — Ferramentas de Análise Fundamentalista e Mercado

Encapsula a busca de dados de mercado (B3) e o cálculo de indicadores
para o Agente Consultor. O LLM apenas decide qual ticker analisar,
e o Python executa a lógica quantitativa pesada.

Nota de performance:
  yfinance é uma biblioteca SÍNCRONA (requests por baixo) e as chamadas ao
  Yahoo Finance levam segundos. A ferramenta é async e delega o trabalho a
  asyncio.to_thread — o event loop segue livre para atender outros usuários
  enquanto os dados de mercado são buscados.
"""
import asyncio
import logging

import yfinance as yf
from pydantic import BaseModel, Field
from langchain_core.tools import tool

_logger = logging.getLogger("myfinance.agent")

# ===========================================================================
# Input Schemas (Pydantic)
# ===========================================================================

class ConsultarAcaoB3Input(BaseModel):
    ticker: str = Field(
        ...,
        description=(
            "O ticker da ação na B3 que o usuário deseja analisar (ex: PETR4, VALE3, WEGE3). "
            "Sempre passe apenas o código de 4 a 6 caracteres. O sistema tratará o sufixo."
        )
    )

# ===========================================================================
# Implementação síncrona (executada em thread separada)
# ===========================================================================

def _buscar_indicadores_sync(ticker: str) -> dict:
    """Corpo síncrono da consulta yfinance — roda via asyncio.to_thread."""
    try:
        # Prepara o ticker para a API do Yahoo Finance (exige .SA para B3)
        ticker_yf = f"{ticker.upper().strip()}.SA"
        acao = yf.Ticker(ticker_yf)
        info = acao.info

        hist = acao.history(period="1y")
        if hist.empty:
            _logger.warning("📈 [TOOL:b3] Ticker '%s' sem dados no Yahoo Finance", ticker)
            return {"erro": f"Nenhum dado encontrado para o ticker {ticker}."}

        preco_atual = hist['Close'].iloc[-1]
        _logger.info(
            "📈 [TOOL:b3] %s → R$ %.2f | P/L %.2f | DY %.2f",
            ticker.upper(), preco_atual,
            info.get('trailingPE', 0.0) or 0.0,
            info.get('dividendYield', 0.0) or 0.0,
        )

        return {
            "ticker": ticker.upper(),
            "preco_atual_brl": round(preco_atual, 2),
            "minima_52_semanas": round(info.get("fiftyTwoWeekLow", 0.0), 2),
            "maxima_52_semanas": round(info.get("fiftyTwoWeekHigh", 0.0), 2),
            "dividend_yield": round(info.get("dividendYield", 0.0), 2),
            "dividend_yield_medio_5_anos": round(info.get("fiveYearAvgDividendYield", 0.0), 2),
            "payout": round(info.get("payoutRatio", 0.0) * 100, 2),
            "divida_bilhoes": round(info.get('totalDebt', 0.0) / 1e9, 2),
            "p_l": round(info.get('trailingPE', 0.0), 2),
            "margem_ebitda": round(info.get('ebitdaMargins', 0.0) * 100, 2),
            "ev_ebitda": round(info.get('enterpriseToEbitda', 0.0), 2),
            "crescimento_receita": round(info.get('revenueGrowth', 0.0) * 100, 2),
            "fluxo_caixa_livre_bilhoes": round(info.get('freeCashflow', 0.0) / 1e9, 2),
            "return_on_equity": round(info.get('returnOnEquity', 0.0) * 100, 2),
            "margem_lucro": round(info.get("profitMargins", 0.0) * 100, 2),
        }

    except Exception as e:
        _logger.error("❌ [TOOL:b3] Falha ao buscar '%s': %s", ticker, e)
        return {"erro": f"Falha ao buscar dados para {ticker}: {str(e)}"}

# ===========================================================================
# Ferramentas
# ===========================================================================

@tool(args_schema=ConsultarAcaoB3Input)
async def consultar_indicadores_b3(ticker: str) -> dict:
    """
    Busca indicadores fundamentalistas e preço atual de uma empresa da B3.
    Use OBRIGATORIAMENTE quando o usuário perguntar sobre
    ações específicas, valuation ou se vale a pena investir em uma empresa.
    """
    return await asyncio.to_thread(_buscar_indicadores_sync, ticker)

# ===========================================================================
# Registro público das ferramentas deste módulo
# ===========================================================================
QUANT_TOOLS = [
    consultar_indicadores_b3,
]
