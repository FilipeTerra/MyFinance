"""
test_fluxo.py — classificar_fluxo é a fonte única de classificação de fluxo de
caixa (criada para eliminar divergência entre calcular_resumo_financeiro,
simular_impacto_nova_despesa e simular_meta_ideal). Estes testes travam a regra
para que nenhum caller volte a reimplementá-la de forma sutilmente diferente.
"""
from src.Application.Agents.Tools.api.fluxo import classificar_fluxo, listar_maiores_despesas


def test_classificar_fluxo_vazio():
    receitas, despesas, aportes = classificar_fluxo([])
    assert (receitas, despesas, aportes) == (0.0, 0.0, 0.0)


def test_classificar_fluxo_type_3_e_aporte_nunca_despesa():
    transacoes = [{"type": 3, "amount": -500.0}]
    receitas, despesas, aportes = classificar_fluxo(transacoes)
    assert aportes == 500.0
    assert despesas == 0.0
    assert receitas == 0.0


def test_classificar_fluxo_type_2_e_despesa():
    transacoes = [{"type": 2, "amount": -150.0}]
    receitas, despesas, aportes = classificar_fluxo(transacoes)
    assert despesas == 150.0
    assert receitas == 0.0
    assert aportes == 0.0


def test_classificar_fluxo_type_1_e_receita():
    transacoes = [{"type": 1, "amount": 3000.0}]
    receitas, despesas, aportes = classificar_fluxo(transacoes)
    assert receitas == 3000.0


def test_classificar_fluxo_type_ausente_usa_sinal_do_valor():
    # type 0/desconhecido: sinal do amount decide despesa vs receita.
    transacoes = [{"type": 0, "amount": -80.0}, {"type": 0, "amount": 200.0}]
    receitas, despesas, aportes = classificar_fluxo(transacoes)
    assert despesas == 80.0
    assert receitas == 200.0


def test_classificar_fluxo_agrega_multiplas_transacoes():
    transacoes = [
        {"type": 1, "amount": 1000.0},
        {"type": 2, "amount": -300.0},
        {"type": 2, "amount": -50.0},
        {"type": 3, "amount": -200.0},
    ]
    receitas, despesas, aportes = classificar_fluxo(transacoes)
    assert receitas == 1000.0
    assert despesas == 350.0
    assert aportes == 200.0


def test_listar_maiores_despesas_ordena_decrescente_e_limita():
    transacoes = [
        {"type": 2, "amount": -50.0, "description": "A", "categoryName": "x"},
        {"type": 2, "amount": -500.0, "description": "B", "categoryName": "y"},
        {"type": 2, "amount": -100.0, "description": "C", "categoryName": "z"},
        {"type": 1, "amount": 5000.0, "description": "Salário"},  # receita: ignorada
    ]
    resultado = listar_maiores_despesas(transacoes, limite=2)
    assert len(resultado) == 2
    assert [r["valor_gasto"] for r in resultado] == [500.0, 100.0]
    assert resultado[0]["descricao"] == "B"


def test_listar_maiores_despesas_sem_despesas_retorna_vazio():
    assert listar_maiores_despesas([{"type": 1, "amount": 100.0}]) == []
