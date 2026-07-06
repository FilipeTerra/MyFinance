"""
test_financial_rag.py — _titulo_livro deriva o nome citável do livro (usado na
citação de fonte "[Fonte: <livro> — <seção>]" que a busca RAG devolve ao agente).
"""
from src.Infra.Data.financial_rag import _titulo_livro


def test_titulo_livro_conhecido():
    assert _titulo_livro("data/books/pai-rico-pai-pobre.txt") == "Pai Rico, Pai Pobre"


def test_titulo_livro_conhecido_ignora_caminho_completo():
    assert (
        _titulo_livro("/qualquer/caminho/o-homem-mais-rico-da-babilonia.txt")
        == "O Homem Mais Rico da Babilônia"
    )


def test_titulo_livro_desconhecido_cai_para_slugify_title_case():
    assert _titulo_livro("novo-livro-financas.txt") == "Novo Livro Financas"


def test_titulo_livro_desconhecido_com_underscore():
    assert _titulo_livro("livro_com_underscore.txt") == "Livro Com Underscore"
