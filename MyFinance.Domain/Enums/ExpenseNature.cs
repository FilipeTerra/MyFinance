namespace MyFinance.Domain.Enums;

/// <summary>
/// Classifica o quanto uma categoria de gasto é cortável quando o usuário precisa
/// liberar dinheiro para uma meta. A classificação é sempre feita pelo próprio
/// usuário: o que é essencial para um (academia de quem depende dela clinicamente)
/// é supérfluo para outro, então o sistema não adivinha.
/// </summary>
public enum ExpenseNature
{
    /// <summary>
    /// Estado inicial de toda categoria. Nunca entra em um plano de corte — sugerir
    /// cortar um gasto que o sistema não sabe o que é seria chute, não conselho.
    /// </summary>
    NaoClassificado = 0,

    /// <summary>Gasto que o usuário considera intocável (moradia, saúde, transporte de trabalho).</summary>
    Essencial = 1,

    /// <summary>Gasto que o usuário aceita reduzir para acelerar uma meta.</summary>
    Discricionario = 2
}
