"""
test_lifestyle_monitor_agent.py — funções puras do monitor de inflação de
estilo de vida: formatação do diagnóstico numérico (nunca gerado pelo LLM) e
o parser regex da resposta do LLM (só extrai CURIOSIDADE/SUGESTAO, nunca números).
"""
from src.Application.Agents.lifestyle_monitor_agent import (
    _montar_informacao,
    _numeros_publicos,
    _parse_resposta_llm,
)


def test_montar_informacao_com_dados_suficientes_usa_variacao():
    dados = {
        "dados_suficientes": True,
        "variacao_estilo_vida_pct": 15.0,
        "variacao_aportes_pct": -5.0,
    }
    texto = _montar_informacao(dados)
    assert "+15%" in texto
    assert "-5%" in texto


def test_montar_informacao_sem_dados_suficientes_usa_percentual_da_renda():
    dados = {
        "dados_suficientes": False,
        "percentual_da_renda_em_estilo_vida": 35.0,
        "media_mensal_estilo_vida": 700.0,
    }
    texto = _montar_informacao(dados)
    # f"{x:,.2f}" formata no padrão US (vírgula milhar, ponto decimal) — não há
    # localização pt-BR na formatação de moeda em nenhum lugar do sistema hoje.
    assert "R$ 700.00" in texto
    assert "35%" in texto


def test_montar_informacao_fallback_so_com_media_mensal():
    dados = {"media_mensal_estilo_vida": 500.0}
    texto = _montar_informacao(dados)
    assert "R$ 500.00" in texto
    assert "%" not in texto


def test_parse_resposta_llm_extrai_ambos_os_campos():
    texto = "CURIOSIDADE: gastar menos ajuda muito\nSUGESTAO: invista antes de gastar"
    curiosidade, sugestao = _parse_resposta_llm(texto)
    assert curiosidade == "gastar menos ajuda muito"
    assert sugestao == "invista antes de gastar"


def test_parse_resposta_llm_aceita_sugestao_com_til_e_minusculo():
    texto = "curiosidade: teste\nsugestão: outra coisa"
    curiosidade, sugestao = _parse_resposta_llm(texto)
    assert curiosidade == "teste"
    assert sugestao == "outra coisa"


def test_parse_resposta_llm_sem_sugestao_retorna_none():
    curiosidade, sugestao = _parse_resposta_llm("CURIOSIDADE: só isso")
    assert curiosidade == "só isso"
    assert sugestao is None


def test_parse_resposta_llm_texto_vazio_retorna_none_none():
    assert _parse_resposta_llm("texto qualquer sem os marcadores") == (None, None)


def test_numeros_publicos_mapeia_chaves_esperadas():
    dados = {
        "percentual_da_renda_em_estilo_vida": 30.0,
        "variacao_estilo_vida_pct": 10.0,
        "variacao_aportes_pct": 2.0,
        "chave_irrelevante": "ignorada",
    }
    assert _numeros_publicos(dados) == {
        "percentual_renda_estilo_vida": 30.0,
        "variacao_estilo_vida_pct": 10.0,
        "variacao_aportes_pct": 2.0,
    }


def test_numeros_publicos_com_dados_ausentes_retorna_none():
    assert _numeros_publicos({}) == {
        "percentual_renda_estilo_vida": None,
        "variacao_estilo_vida_pct": None,
        "variacao_aportes_pct": None,
    }
