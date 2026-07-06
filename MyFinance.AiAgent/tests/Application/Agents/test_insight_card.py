import pytest

from src.Application.Agents.insight_card import InsightCard, erro_result


def test_insight_card_to_dict():
    card = InsightCard(
        curiosidade="c",
        informacao="i",
        sugestao="s",
    )
    assert card.to_dict() == {"curiosidade": "c", "informacao": "i", "sugestao": "s"}


def test_insight_card_e_imutavel():
    card = InsightCard(curiosidade="c", informacao="i", sugestao="s")
    with pytest.raises(Exception):
        card.curiosidade = "outro"


def test_erro_result():
    assert erro_result("deu ruim") == {"success": False, "erro": "deu ruim"}
