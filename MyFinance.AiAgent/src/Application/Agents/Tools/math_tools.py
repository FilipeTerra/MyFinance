from langchain_core.tools import tool


@tool
def simular_juros_compostos(
    capital_inicial: float,
    aporte_mensal: float,
    taxa_juros_anual: float,
    meses: int,
) -> str:
    """Use esta ferramenta OBRIGATORIAMENTE sempre que o usuário pedir para projetar valores
    no futuro, calcular rendimentos, simular investimentos ou estimar quanto terá em N meses/anos.
    Recebe capital inicial, aporte mensal, taxa anual (em %) e prazo em meses.
    Retorna o valor final, total investido e o ganho gerado pelos juros."""
    if meses <= 0:
        return "Erro: o prazo deve ser maior que zero."
    if taxa_juros_anual < 0:
        return "Erro: taxa de juros não pode ser negativa."

    taxa_mensal = (1 + taxa_juros_anual / 100) ** (1 / 12) - 1
    montante = capital_inicial * (1 + taxa_mensal) ** meses

    if taxa_mensal > 0:
        montante += aporte_mensal * ((1 + taxa_mensal) ** meses - 1) / taxa_mensal
    else:
        montante += aporte_mensal * meses

    total_investido = capital_inicial + (aporte_mensal * meses)
    ganho_juros = montante - total_investido

    return (
        f"📊 Simulação de Juros Compostos\n"
        f"  • Capital inicial: R$ {capital_inicial:,.2f}\n"
        f"  • Aporte mensal:   R$ {aporte_mensal:,.2f}\n"
        f"  • Taxa anual:      {taxa_juros_anual:.2f}% a.a.\n"
        f"  • Prazo:           {meses} meses ({meses / 12:.1f} anos)\n"
        f"\n"
        f"  💰 Total investido: R$ {total_investido:,.2f}\n"
        f"  📈 Ganho em juros:  R$ {ganho_juros:,.2f}\n"
        f"  🏆 Valor final:     R$ {montante:,.2f}"
    )


@tool
def comparar_quitacao_vs_investimento(
    valor_divida: float,
    taxa_juros_divida_anual: float,
    taxa_rendimento_investimento_anual: float,
) -> str:
    """Use esta ferramenta OBRIGATORIAMENTE quando o usuário perguntar se deve pagar uma dívida
    ou investir o dinheiro. Recebe o valor da dívida, a taxa anual da dívida e a taxa anual
    do investimento pretendido. Retorna a recomendação matemática baseada nos números."""
    if taxa_juros_divida_anual <= 0 or taxa_rendimento_investimento_anual <= 0:
        return "Erro: as taxas devem ser maiores que zero."

    custo_divida_mensal = (1 + taxa_juros_divida_anual / 100) ** (1 / 12) - 1
    retorno_invest_mensal = (1 + taxa_rendimento_investimento_anual / 100) ** (1 / 12) - 1

    divida_em_12m = valor_divida * (1 + custo_divida_mensal) ** 12
    invest_em_12m = valor_divida * (1 + retorno_invest_mensal) ** 12

    custo_nao_quitar = divida_em_12m - valor_divida
    ganho_investimento = invest_em_12m - valor_divida
    diferenca = abs(custo_nao_quitar - ganho_investimento)

    if taxa_juros_divida_anual > taxa_rendimento_investimento_anual:
        recomendacao = "✅ QUITAR A DÍVIDA primeiro"
        motivo = (
            f"A dívida custa {taxa_juros_divida_anual:.1f}% a.a., "
            f"mais caro que o retorno de {taxa_rendimento_investimento_anual:.1f}% a.a. do investimento. "
            f"Quitar economiza R$ {diferenca:,.2f} em 12 meses."
        )
    else:
        recomendacao = "📈 INVESTIR pode ser mais vantajoso"
        motivo = (
            f"O investimento rende {taxa_rendimento_investimento_anual:.1f}% a.a., "
            f"superior ao custo da dívida de {taxa_juros_divida_anual:.1f}% a.a. "
            f"O ganho líquido em 12 meses seria de R$ {diferenca:,.2f}."
        )

    return (
        f"⚖️ Comparativo: Quitar vs. Investir\n"
        f"  • Valor analisado:         R$ {valor_divida:,.2f}\n"
        f"  • Custo da dívida (a.a.):  {taxa_juros_divida_anual:.2f}%\n"
        f"  • Retorno invest. (a.a.):  {taxa_rendimento_investimento_anual:.2f}%\n"
        f"\n"
        f"  📌 Em 12 meses a dívida custaria: R$ {custo_nao_quitar:,.2f}\n"
        f"  📌 Em 12 meses o invest. renderia: R$ {ganho_investimento:,.2f}\n"
        f"\n"
        f"  🏁 Recomendação: {recomendacao}\n"
        f"  {motivo}"
    )
