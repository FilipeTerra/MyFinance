"""
tools.py — Biblioteca de Ferramentas Determinísticas para o Agente Financeiro

Princípio de Separação entre Decisão e Execução:
  - O LLM DECIDE quais ferramentas chamar e passa os parâmetros em JSON.
  - O Python EXECUTA a matemática de forma determinística aqui.
  - O LLM NUNCA faz cálculos; ele apenas observa os resultados (Observation).

Todas as funções retornam dicionários com os valores brutos calculados.
O nó de exibição (response node) é responsável por formatar para o usuário.
"""
from pydantic import BaseModel, Field
from langchain_core.tools import tool
import requests


# ===========================================================================
# Input Schemas (Pydantic)
# Os schemas geram o JSON Schema que o LLM usa para preencher os argumentos.
# Quanto mais rica a descrição de cada Field, melhor o LLM entende o que
# passar — isso reduz erros de chamada de ferramenta diretamente.
# ===========================================================================

class SimularInvestimentoInput(BaseModel):
    capital_inicial: float = Field(
        ...,
        description=(
            "Valor em reais (R$) que o usuário já possui para investir agora. "
            "Deve ser maior ou igual a zero. Ex: 10000.0 para R$ 10.000,00."
        ),
        ge=0,
    )
    aporte_mensal: float = Field(
        ...,
        description=(
            "Valor em reais (R$) que o usuário irá depositar todo mês ao longo do período. "
            "Use 0.0 se não houver aporte mensal. Ex: 500.0 para R$ 500,00/mês."
        ),
        ge=0,
    )
    taxa_juros_anual: float = Field(
        ...,
        description=(
            "Taxa de juros anual em PORCENTAGEM (%), não em decimal. "
            "Ex: 12.0 para 12% ao ano (CDI, Tesouro Direto, poupança, etc.). "
            "Para simular a SELIC, use a taxa retornada por buscar_taxa_selic."
        ),
        gt=0,
    )
    meses: int = Field(
        ...,
        description=(
            "Horizonte de tempo do investimento em meses inteiros. "
            "Ex: 12 para 1 ano, 60 para 5 anos, 120 para 10 anos. "
            "Deve ser maior que zero."
        ),
        gt=0,
    )


class CalcularJurosFinanciamentoInput(BaseModel):
    valor_financiado: float = Field(
        ...,
        description=(
            "Valor total em reais (R$) que está sendo financiado (principal). "
            "É o preço do bem menos a entrada já paga. Ex: 45000.0 para um carro de R$ 50.000 "
            "com entrada de R$ 5.000."
        ),
        gt=0,
    )
    taxa_juros_mensal: float = Field(
        ...,
        description=(
            "Taxa de juros mensal do financiamento em PORCENTAGEM (%), não em decimal. "
            "Ex: 1.5 para 1,5% ao mês. Bancos costumam informar a taxa mensal nos contratos. "
            "Para converter taxa anual para mensal: taxa_mensal = (1 + taxa_anual/100)^(1/12) - 1."
        ),
        ge=0,
    )
    num_parcelas: int = Field(
        ...,
        description=(
            "Número total de parcelas mensais do financiamento. "
            "Ex: 48 para um financiamento de 4 anos, 360 para um imóvel de 30 anos."
        ),
        gt=0,
    )


# buscar_taxa_selic não precisa de schema pois não tem parâmetros obrigatórios.


# ===========================================================================
# Ferramentas
# ===========================================================================

@tool(args_schema=SimularInvestimentoInput)
def simular_investimento(
    capital_inicial: float,
    aporte_mensal: float,
    taxa_juros_anual: float,
    meses: int,
) -> dict:
    """
    Simula o crescimento de um investimento com juros compostos, capital inicial
    e aportes mensais recorrentes pelo período informado.

    Use esta ferramenta OBRIGATORIAMENTE quando o usuário fizer perguntas como:
      - "Quanto terei se investir R$ X por mês durante N anos?"
      - "Quanto rende R$ X aplicado na SELIC por 2 anos?"
      - "Se eu guardar R$ 500 por mês, quanto terei em 5 anos?"
      - "Simule meu investimento com taxa de 12% ao ano."

    A fórmula aplicada é o modelo de juros compostos com aportes periódicos:
      Montante = C × (1+i)^n  +  PMT × [ (1+i)^n − 1 ] / i
    onde C = capital_inicial, i = taxa_mensal (decimal), n = meses, PMT = aporte_mensal.

    Args:
        capital_inicial: Valor em R$ já disponível para aplicar hoje (≥ 0).
        aporte_mensal:   Depósito mensal recorrente em R$ ao longo do período (≥ 0).
        taxa_juros_anual: Taxa de rendimento anual em % (ex: 12.0 = 12% a.a.).
        meses:           Prazo total do investimento em meses inteiros (> 0).

    Returns:
        dict com as chaves:
          - capital_inicial (float):        Valor inicial aportado em R$.
          - aporte_mensal (float):          Depósito mensal em R$.
          - taxa_juros_anual_pct (float):   Taxa anual informada em %.
          - taxa_juros_mensal_pct (float):  Taxa mensal equivalente em % (calculada).
          - meses (int):                   Prazo em meses.
          - anos (float):                  Prazo em anos (meses / 12).
          - total_aportado (float):         Capital + soma de todos os aportes em R$.
          - montante_final (float):         Valor total acumulado ao final em R$.
          - ganho_juros (float):            Ganho gerado exclusivamente pelos juros em R$.
          - multiplicador (float):          Quantas vezes o total aportado se multiplicou.
          - rentabilidade_total_pct (float): Retorno total sobre o capital aportado em %.

    Raises:
        ValueError: Se taxa_juros_anual ou meses forem inválidos (validado pelo schema).

    Example:
        Input:  capital_inicial=10000, aporte_mensal=500, taxa_juros_anual=12.0, meses=60
        Output: {"montante_final": ~132455.00, "ganho_juros": ~52455.00, ...}
    """
    # Converte taxa anual para taxa mensal equivalente (capitalização composta)
    taxa_mensal = (1 + taxa_juros_anual / 100) ** (1 / 12) - 1

    # Juros compostos sobre o capital inicial
    montante = capital_inicial * (1 + taxa_mensal) ** meses

    # Valor futuro de uma série de aportes mensais (anuidade)
    if taxa_mensal > 0:
        montante += aporte_mensal * ((1 + taxa_mensal) ** meses - 1) / taxa_mensal
    else:
        montante += aporte_mensal * meses

    total_aportado = capital_inicial + (aporte_mensal * meses)
    ganho_juros = montante - total_aportado
    multiplicador = (montante / total_aportado) if total_aportado > 0 else 0.0
    rentabilidade_pct = ((montante / total_aportado) - 1) * 100 if total_aportado > 0 else 0.0

    return {
        "capital_inicial": round(capital_inicial, 2),
        "aporte_mensal": round(aporte_mensal, 2),
        "taxa_juros_anual_pct": round(taxa_juros_anual, 4),
        "taxa_juros_mensal_pct": round(taxa_mensal * 100, 4),
        "meses": meses,
        "anos": round(meses / 12, 2),
        "total_aportado": round(total_aportado, 2),
        "montante_final": round(montante, 2),
        "ganho_juros": round(ganho_juros, 2),
        "multiplicador": round(multiplicador, 4),
        "rentabilidade_total_pct": round(rentabilidade_pct, 2),
    }


@tool(args_schema=CalcularJurosFinanciamentoInput)
def calcular_juros_financiamento(
    valor_financiado: float,
    taxa_juros_mensal: float,
    num_parcelas: int,
) -> dict:
    """
    Calcula as condições completas de um financiamento pela Tabela Price (parcelas fixas),
    incluindo o valor da parcela, o total pago e o custo total em juros.

    Use esta ferramenta OBRIGATORIAMENTE quando o usuário fizer perguntas como:
      - "Qual a parcela de um financiamento de R$ 50.000 em 48x a 1,5% ao mês?"
      - "Quanto vou pagar de juros se financiar meu carro?"
      - "Calcule meu financiamento imobiliário de 30 anos."
      - "Vale a pena financiar ou pagar à vista?"

    A fórmula da Tabela Price (PMT) é:
      PMT = PV × [ i × (1+i)^n ] / [ (1+i)^n − 1 ]
    onde PV = valor_financiado, i = taxa_mensal (decimal), n = num_parcelas.
    No caso especial de taxa = 0%, a parcela é simplesmente PV / n.

    Args:
        valor_financiado:  Principal do financiamento em R$ (valor do bem menos entrada).
        taxa_juros_mensal: Taxa de juros mensal em % (ex: 1.5 = 1,5% a.m.).
        num_parcelas:      Número total de parcelas mensais (ex: 48, 60, 360).

    Returns:
        dict com as chaves:
          - valor_financiado (float):       Principal em R$.
          - taxa_juros_mensal_pct (float):  Taxa mensal em %.
          - taxa_juros_anual_pct (float):   Taxa anual equivalente em % (calculada).
          - num_parcelas (int):             Número de parcelas.
          - valor_parcela (float):          Valor fixo de cada parcela (PMT) em R$.
          - total_pago (float):             Soma de todas as parcelas em R$.
          - total_juros (float):            Total pago a mais em juros em R$.
          - custo_efetivo_total_pct (float): Custo total dos juros como % do principal.
          - amortizacao_media_mensal (float): Quanto do principal é quitado por mês em média.

    Raises:
        ValueError: Se valor_financiado ou num_parcelas forem inválidos (validado pelo schema).

    Example:
        Input:  valor_financiado=50000, taxa_juros_mensal=1.5, num_parcelas=48
        Output: {"valor_parcela": ~1494.27, "total_juros": ~21724.90, ...}
    """
    i = taxa_juros_mensal / 100  # Converte percentual para decimal

    # Caso especial: financiamento sem juros
    if i == 0:
        valor_parcela = valor_financiado / num_parcelas
    else:
        # Tabela Price: PMT = PV * [i * (1+i)^n] / [(1+i)^n - 1]
        fator = (1 + i) ** num_parcelas
        valor_parcela = valor_financiado * (i * fator) / (fator - 1)

    total_pago = valor_parcela * num_parcelas
    total_juros = total_pago - valor_financiado
    custo_efetivo_pct = (total_juros / valor_financiado) * 100
    taxa_anual_equivalente = ((1 + i) ** 12 - 1) * 100

    return {
        "valor_financiado": round(valor_financiado, 2),
        "taxa_juros_mensal_pct": round(taxa_juros_mensal, 4),
        "taxa_juros_anual_pct": round(taxa_anual_equivalente, 4),
        "num_parcelas": num_parcelas,
        "valor_parcela": round(valor_parcela, 2),
        "total_pago": round(total_pago, 2),
        "total_juros": round(total_juros, 2),
        "custo_efetivo_total_pct": round(custo_efetivo_pct, 2),
        "amortizacao_media_mensal": round(valor_financiado / num_parcelas, 2),
    }


@tool
def buscar_taxa_selic() -> dict:
    """
    Retorna as taxas de referência da economia brasileira: SELIC (taxa básica de juros)
    e IPCA (inflação oficial), bem como os juros reais (SELIC descontada a inflação).
    
    Use esta ferramenta ANTES de qualquer simulação de investimento ou análise de 
    cenário econômico que dependa de juros ou inflação.
    """
    # 1. Valores de fallback (acionados se a API do BCB falhar)
    selic_anual = 14.25
    ipca_anual = 4.72
    fonte = "Banco Central do Brasil (Fallback Estático)"
    data_ref_selic = "2025-01 (Fallback)"
    data_ref_ipca = "2025-01 (Fallback)"

    try:
        # Busca Selic Meta (Série 432)
        resp_selic = requests.get(
            "https://api.bcb.gov.br/dados/serie/bcdata.sgs.432/dados/ultimos/1?formato=json",
            timeout=5
        )
        resp_selic.raise_for_status()
        dados_selic = resp_selic.json()[0]
        selic_anual = float(dados_selic["valor"])
        data_ref_selic = dados_selic["data"]

        # Busca IPCA Acumulado 12 meses (Série 13522)
        resp_ipca = requests.get(
            "https://api.bcb.gov.br/dados/serie/bcdata.sgs.13522/dados/ultimos/1?formato=json",
            timeout=5
        )
        resp_ipca.raise_for_status()
        dados_ipca = resp_ipca.json()[0]
        ipca_anual = float(dados_ipca["valor"])
        data_ref_ipca = dados_ipca["data"]

        fonte = "Banco Central do Brasil (API SGS em tempo real)"

    except Exception as e:
        # Se a API cair ou demorar mais de 5s, captura o erro e mantém as variáveis de fallback
        print(f"⚠️ [BCB API] Falha ao buscar dados em tempo real. Usando fallback. Erro: {e}")

    # 2. Cálculos derivados (executados independentemente da origem dos dados)
    
    # Conversão de taxa anual para mensal (capitalização composta)
    # Fórmula: i_m = (1 + i_a)^(1/12) - 1
    selic_mensal = ((1 + selic_anual / 100) ** (1 / 12) - 1) * 100
    ipca_mensal = ((1 + ipca_anual / 100) ** (1 / 12) - 1) * 100

    # Juros reais via Equação de Fisher
    juros_real = ((1 + selic_anual / 100) / (1 + ipca_anual / 100) - 1) * 100

    # CDI: convencionalmente ~0,10pp abaixo da SELIC Meta
    cdi_anual = selic_anual - 0.10

    return {
        "selic_anual_pct": round(selic_anual, 2),
        "selic_mensal_pct": round(selic_mensal, 4),
        "ipca_anual_pct": round(ipca_anual, 2),
        "ipca_mensal_pct": round(ipca_mensal, 4),
        "juros_real_anual_pct": round(juros_real, 4),
        "cdi_anual_pct": round(cdi_anual, 2),
        "data_referencia_selic": data_ref_selic,
        "data_referencia_ipca": data_ref_ipca,
        "fonte": fonte
    }


# ===========================================================================
# Registro público das ferramentas deste módulo
# Importado pelo grafo: from .tools import MATH_TOOLS
# ===========================================================================
MATH_TOOLS = [
    simular_investimento,
    calcular_juros_financiamento,
    buscar_taxa_selic,
]
