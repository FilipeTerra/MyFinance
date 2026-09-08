using System;

namespace MyFinance.Domain.Entities;

/// <summary>
/// Associação aprendida entre a descrição de um lançamento e a categoria que o
/// usuário escolheu para ela. É o que permite que uma importação futura já venha
/// classificada — antes isso vivia num arquivo JSON do agente de IA, o que fazia
/// todo o aprendizado sumir quando o agente estava fora do ar.
/// </summary>
public class CategoryRule
{
    /// <summary>
    /// Descrição normalizada do lançamento (ver StatementTextNormalizer). Guardar
    /// já normalizado é o que torna a busca um índice exato em vez de varredura.
    /// </summary>
    public string DescriptionKey { get; private set; } = string.Empty;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Construtor usado pelo EF Core na materialização.</summary>
    private CategoryRule() { }

    public CategoryRule(Guid userId, string descriptionKey, Guid categoryId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A regra precisa pertencer a um usuário.", nameof(userId));

        if (string.IsNullOrWhiteSpace(descriptionKey))
            throw new ArgumentException("A regra precisa de uma descrição.", nameof(descriptionKey));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("A regra precisa apontar para uma categoria.", nameof(categoryId));

        Id = Guid.NewGuid();
        UserId = userId;
        DescriptionKey = descriptionKey;
        CategoryId = categoryId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Repõe a categoria da regra quando o usuário reclassifica o mesmo lançamento.</summary>
    public void PointTo(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("A regra precisa apontar para uma categoria.", nameof(categoryId));

        if (CategoryId == categoryId)
            return;

        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }
}
