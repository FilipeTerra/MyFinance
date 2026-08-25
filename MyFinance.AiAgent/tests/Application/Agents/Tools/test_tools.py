"""
test_tools.py — matemática financeira determinística (juros compostos e
Tabela Price). São @tool (StructuredTool), então chamamos via .invoke(dict) —
o mesmo caminho usado pelo LLM — em vez de acessar a função Python crua.
"""
import pytest

from src.Application.Agents.Tools.tools import (
    _buscar_taxa_selic_impl,
    calcular_juros_financiamento,
    simular_investimento,
)


def test_simular_investimento_valores_conhecidos():
    # C=10000, aporte=500/mês, 12% a.a., 60 meses (5 anos).
    # (1+i)^60 = (1.12)^5 exatamente, pois i é a taxa mensal equivalente a 12% a.a.
    resultado = simular_investimento.invoke({
        "capital_inicial": 10000,
        "aporte_mensal": 500,
        "taxa_juros_anual": 12.0,
        "meses": 60,
    })
    assert resultado["montante_final"] == pytest.approx(57794.05, rel=1e-4)
    assert resultado["total_aportado"] == 10000 + 500 * 60
    assert resultado["ganho_juros"] == pytest.approx(
        resultado["montante_final"] - resultado["total_aportado"]
    )
    assert resultado["anos"] == 5.0


def test_simular_investimento_sem_aporte_mensal_cresce_so_o_capital():
    resultado = simular_investimento.invoke({
        "capital_inicial": 1000,
        "aporte_mensal": 0,
        "taxa_juros_anual": 12.0,
        "meses": 12,
    })
    # Após 12 meses à taxa mensal equivalente a 12% a.a., o capital vira exatamente capital*1.12.
    assert resultado["montante_final"] == pytest.approx(1120.0, rel=1e-4)
    assert resultado["total_aportado"] == 1000


def test_calcular_juros_financiamento_valores_conhecidos():
    # Tabela Price: PV=50000, i=1.5% a.m., 48 parcelas.
    resultado = calcular_juros_financiamento.invoke({
        "valor_financiado": 50000,
        "taxa_juros_mensal": 1.5,
        "num_parcelas": 48,
    })
    assert resultado["valor_parcela"] == pytest.approx(1468.75, rel=1e-3)
    assert resultado["total_pago"] == pytest.approx(
        resultado["valor_parcela"] * 48
    )
    assert resultado["total_juros"] == pytest.approx(
        resultado["total_pago"] - 50000
    )


def test_calcular_juros_financiamento_taxa_zero_e_divisao_simples():
    resultado = calcular_juros_financiamento.invoke({
        "valor_financiado": 12000,
        "taxa_juros_mensal": 0,
        "num_parcelas": 12,
    })
    assert resultado["valor_parcela"] == pytest.approx(1000.0)
    assert resultado["total_juros"] == pytest.approx(0.0)


async def test_buscar_taxa_selic_impl_retorna_dict_com_chaves_esperadas():
    # Chamada direta à implementação pura (sem @tool) — mesma função reaproveitada
    # pelo endpoint REST GET /api/market/selic no backend .NET.
    resultado = await _buscar_taxa_selic_impl()

    assert "selic_anual_pct" in resultado
    assert "ipca_anual_pct" in resultado
    assert "cdi_anual_pct" in resultado
    assert "juros_real_anual_pct" in resultado
    assert "fonte" in resultado
    assert resultado["selic_anual_pct"] > 0
