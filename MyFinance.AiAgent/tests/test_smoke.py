"""
test_smoke.py — Garante que o composition root (main.py) importa sem erro.

Automatiza o `import src.Api.main` que foi rodado manualmente após CADA
mudança na camada AiAgent durante o refactor — se algum import quebrar em
qualquer módulo do grafo de dependências, este teste falha primeiro.
"""


def test_main_importa_sem_erro():
    import src.Api.main  # noqa: F401
