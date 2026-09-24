namespace MyFinance.Application.Dtos.Sugestao;

/// <summary>
/// De onde a renda mensal usada na sugestão foi obtida. A UI usa isso para
/// explicar o número e, quando for o caso, convidar o usuário a cadastrar o salário.
/// </summary>
public enum FonteRenda
{
    /// <summary>Não foi possível determinar a renda: sem salário no perfil e sem receitas lançadas.</summary>
    Indisponivel = 0,

    /// <summary>Salário informado pelo usuário no perfil (<c>User.MonthlyIncome</c>).</summary>
    PerfilDeclarado = 1,

    /// <summary>Média das receitas efetivamente lançadas no período analisado.</summary>
    TransacoesRealizadas = 2
}
