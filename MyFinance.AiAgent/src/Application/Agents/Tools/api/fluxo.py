"""
fluxo.py — Classificação de fluxo de caixa a partir de transações do .NET.

Funções puras (sem I/O), reusadas pelas tools de análise em transacoes.py,
metas.py e proativas.py.
"""


def classificar_fluxo(transacoes: list) -> tuple[float, float, float]:
    """
    Classificador ÚNICO de fluxo de caixa — fonte da verdade para todas as
    tools de análise (resumo, impacto de despesa, meta ideal).

    Regra (espelha TransactionType do .NET):
      type 3 (Investment)          → aporte em meta/investimento (patrimônio,
                                     NÃO é despesa — o dinheiro continua do usuário)
      type 2 ou valor negativo     → despesa
      type 1 ou valor positivo     → receita

    Antes deste helper cada tool duplicava a classificação com regras
    ligeiramente diferentes (o simulador contava aporte como despesa; o resumo
    não), produzindo números conflitantes entre respostas do agente.

    Returns:
        (receitas, despesas, aportes) — todos como valores absolutos.
    """
    receitas = despesas = aportes = 0.0
    for t in transacoes:
        tx_type = t.get("type", 0)
        amount = t.get("amount", 0)
        if tx_type == 3:
            aportes += abs(amount)
        elif tx_type == 2 or (tx_type not in (1, 3) and amount < 0):
            despesas += abs(amount)
        elif tx_type == 1 or (tx_type not in (2, 3) and amount > 0):
            receitas += amount
    return receitas, despesas, aportes


def listar_maiores_despesas(transacoes: list, limite: int = 8) -> list:
    """Retorna as N maiores despesas do mês para que o LLM analise a essencialidade."""
    despesas = []
    for t in transacoes:
        if t.get("type") == 2 or t.get("amount", 0) < 0:
            despesas.append({
                "descricao": t.get("description", "Despesa não identificada"),
                "categoria": str(t.get("categoryName", "Outros")).title(),
                "valor_gasto": round(abs(t.get("amount", 0)), 2)
            })
    return sorted(despesas, key=lambda x: x["valor_gasto"], reverse=True)[:limite]
