"""
test_proactive_analyzer_agent.py — _montar_conteudo é a única fonte de verdade
sobre QUANDO exibir o card de reserva de emergência e QUAL variante mostrar
(aviso vs. info). Uma regressão aqui muda o que aparece no dashboard do usuário.
"""
from src.Application.Agents import proactive_analyzer_agent as m


def test_reserva_adequada_nao_exibe_card():
    resultado = m._montar_conteudo({"reserva_adequada": True})
    assert resultado == {"exibir_card": False}


def test_ja_iniciou_com_meta_mostra_card_aviso():
    dados = {
        "reserva_adequada": False,
        "possui_meta_reserva": True,
        "possui_investimento_renda_fixa": False,
        "percentual_atingido": 40.0,
        "valor_faltante": 6000.0,
    }
    resultado = m._montar_conteudo(dados)
    assert resultado["exibir_card"] is True
    assert resultado["tipo_card"] == "aviso"
    assert resultado["curiosidade"] == m._CURIOSIDADE_EM_ANDAMENTO
    assert "40%" in resultado["informacao"]
    # f"{x:,.2f}" formata no padrão US (vírgula milhar, ponto decimal).
    assert "6,000.00" in resultado["informacao"]


def test_ja_iniciou_com_investimento_renda_fixa_tambem_mostra_aviso():
    dados = {
        "reserva_adequada": False,
        "possui_meta_reserva": False,
        "possui_investimento_renda_fixa": True,
        "percentual_atingido": 80.0,
        "valor_faltante": 1000.0,
    }
    resultado = m._montar_conteudo(dados)
    assert resultado["tipo_card"] == "aviso"


def test_nao_iniciou_nada_mostra_card_info():
    dados = {
        "reserva_adequada": False,
        "possui_meta_reserva": False,
        "possui_investimento_renda_fixa": False,
        "valor_ideal_reserva": 24000.0,
    }
    resultado = m._montar_conteudo(dados)
    assert resultado["exibir_card"] is True
    assert resultado["tipo_card"] == "info"
    assert resultado["curiosidade"] == m._CURIOSIDADE_NAO_INICIADA
    assert "24,000.00" in resultado["informacao"]
