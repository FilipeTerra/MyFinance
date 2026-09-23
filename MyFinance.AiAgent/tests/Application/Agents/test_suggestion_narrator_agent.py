"""
test_suggestion_narrator_agent.py — funções puras do redator da sugestão de aporte.

O foco é o bloco de fatos: é ele que delimita o que o LLM tem permissão para
citar. Um valor que não aparece aqui e mesmo assim sai na resposta faz a API
.NET descartar o texto inteiro, então o que este bloco expõe (e como formata)
é parte do contrato entre os dois lados.
"""
import pytest

from src.Application.Agents.suggestion_narrator_agent import (
    _moeda,
    _montar_bloco_de_fatos,
    narrate_suggestion,
)


def test_moeda_usa_formato_brasileiro():
    # A API .NET valida os valores da resposta com a cultura pt-BR: ponto de
    # milhar e vírgula decimal. Formatar em padrão US faria todo texto ser recusado.
    assert _moeda(5000) == "R$ 5.000,00"
    assert _moeda(1234567.89) == "R$ 1.234.567,89"
    assert _moeda(0) == "R$ 0,00"
    assert _moeda(99.5) == "R$ 99,50"


def test_bloco_de_fatos_quando_cabe_mostra_folga_e_omite_deficit():
    bloco = _montar_bloco_de_fatos({
        "aporteMensalNecessario": 500.0,
        "cabe": True,
        "rendaMensal": 5000.0,
        "despesaMensalMedia": 2000.0,
        "sobraLivre": 3000.0,
        "folgaRestante": 2500.0,
        "percentualDaRenda": 10.0,
    })

    assert "O aporte cabe no orçamento: sim" in bloco
    assert "Folga que ainda sobra depois do aporte: R$ 2.500,00" in bloco
    assert "Falta por mês" not in bloco
    assert "10.0% da renda" in bloco


def test_bloco_de_fatos_quando_nao_cabe_mostra_deficit_e_cortes():
    bloco = _montar_bloco_de_fatos({
        "aporteMensalNecessario": 2000.0,
        "cabe": False,
        "rendaMensal": 5000.0,
        "despesaMensalMedia": 4800.0,
        "sobraLivre": 200.0,
        "deficit": 1800.0,
        "cortes": [
            {"categoria": "Delivery", "valorCorte": 150.0},
            {"categoria": "Streaming", "valorCorte": 50.0},
        ],
    })

    assert "O aporte cabe no orçamento: não" in bloco
    assert "Falta por mês para bancar o aporte: R$ 1.800,00" in bloco
    assert "- Delivery: R$ 150,00 por mês" in bloco
    assert "- Streaming: R$ 50,00 por mês" in bloco
    assert "Folga que ainda sobra" not in bloco


def test_bloco_de_fatos_inclui_reserva_e_cenarios_quando_presentes():
    bloco = _montar_bloco_de_fatos({
        "cabe": False,
        "reservaFaltante": 24000.0,
        "prazoMesesAlternativo": 84,
        "valorAlvoAlternativo": 12000.0,
    })

    assert "falta R$ 24.000,00" in bloco
    assert "em 84 meses" in bloco
    assert "alcançaria R$ 12.000,00" in bloco


def test_bloco_de_fatos_omite_campos_ausentes():
    # Campos nulos simplesmente não aparecem: uma linha "falta R$ 0,00" seria lida
    # pelo LLM como fato, e ele repetiria um número que não quer dizer nada.
    bloco = _montar_bloco_de_fatos({"cabe": True, "rendaMensal": 3000.0})

    assert "Reserva de emergência" not in bloco
    assert "Alternativa" not in bloco
    assert "Cortes sugeridos" not in bloco


def test_bloco_de_fatos_e_delimitado():
    bloco = _montar_bloco_de_fatos({"cabe": True})

    assert bloco.startswith("=== FATOS JÁ CALCULADOS")
    assert bloco.rstrip().endswith("=== FIM DOS FATOS ===")


@pytest.mark.asyncio
async def test_narrate_suggestion_com_llm_fora_devolve_erro_sem_levantar(monkeypatch):
    def _explode(*_args, **_kwargs):
        raise RuntimeError("ollama fora do ar")

    monkeypatch.setattr(
        "src.Application.Agents.suggestion_narrator_agent.get_chat_llm", _explode
    )

    resultado = await narrate_suggestion({"cabe": True, "rendaMensal": 1000.0})

    # Sem texto a API .NET usa o template determinístico — a falha não pode virar exceção.
    assert resultado["success"] is False
    assert "erro" in resultado
    assert "ollama" not in resultado["erro"].lower()
