"""
test_session.py — resolver_periodo converte parâmetros do usuário (datas livres
ou "últimos N dias") num intervalo concreto. É chamada por quase todas as tools
de análise (consultar_transacoes_recentes, analisar_gastos_por_categoria,
calcular_resumo_financeiro...), então um bug aqui afeta tudo de uma vez.

Os casos "sem data" usam datetime.now() internamente — em vez de congelar o
relógio, testamos a RELAÇÃO entre dt_inicio/dt_fim (que é determinística
independente do instante real em que o teste roda).
"""
from datetime import datetime, timedelta, timezone

from src.Application.Agents.Tools.api.session import resolver_periodo


def test_sem_datas_usa_janela_relativa_a_ultimos_dias():
    dt_i, dt_f, label, dias_periodo = resolver_periodo("", "", ultimos_dias=30)
    assert dt_f - dt_i == timedelta(days=30)
    assert label == "últimos 30 dias"
    assert dias_periodo == 31  # 30 dias completos + o dia corrente


def test_sem_datas_dt_fim_e_proximo_de_agora():
    antes = datetime.now(timezone.utc)
    _, dt_f, _, _ = resolver_periodo("", "")
    depois = datetime.now(timezone.utc)
    assert antes <= dt_f <= depois


def test_com_data_inicio_e_fim_validas():
    dt_i, dt_f, label, dias_periodo = resolver_periodo("2026-05-01", "2026-05-31")
    assert dt_i == datetime(2026, 5, 1, tzinfo=timezone.utc)
    assert dt_f == datetime(2026, 5, 31, 23, 59, 59, tzinfo=timezone.utc)
    assert label == "01/05/2026 a 31/05/2026"
    assert dias_periodo == 31


def test_com_data_inicio_e_sem_data_fim_usa_agora_como_fim():
    dt_i, dt_f, label, _ = resolver_periodo("2026-05-01", "")
    assert dt_i == datetime(2026, 5, 1, tzinfo=timezone.utc)
    assert abs((dt_f - datetime.now(timezone.utc)).total_seconds()) < 5
    assert label.startswith("01/05/2026 a ")


def test_data_fim_invalida_cai_para_agora():
    dt_i, dt_f, _, _ = resolver_periodo("2026-05-01", "não-é-uma-data")
    assert dt_i == datetime(2026, 5, 1, tzinfo=timezone.utc)
    assert abs((dt_f - datetime.now(timezone.utc)).total_seconds()) < 5


def test_data_inicio_invalida_cai_para_janela_relativa():
    # data_inicio não vazia mas mal formatada: o parse falha e cai no fallback
    # (agora - ultimos_dias), mas o rótulo ainda usa o formato "X a Y" — o
    # branch do label é decidido por data_inicio.strip() ser não-vazia, não
    # pelo sucesso do parse. Comportamento atual documentado aqui.
    dt_i, dt_f, label, _ = resolver_periodo("data-invalida", "", ultimos_dias=10)
    assert dt_f - dt_i == timedelta(days=10)
    assert " a " in label
