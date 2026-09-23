using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid UserId { get; set; } // A Categoria pertence a UM usuário
    public User User { get; set; } = null!;

    /// <summary>
    /// Quão cortável é o gasto desta categoria. Só muda por <see cref="Reclassify"/>,
    /// porque é o usuário quem decide o que é essencial na vida dele — nenhum fluxo
    /// do sistema pode reclassificar por conta própria.
    /// </summary>
    public ExpenseNature Nature { get; private set; } = ExpenseNature.NaoClassificado;

    public Category(string name, Guid userId)
    {
        Id = Guid.NewGuid(); // Gerar um novo Id para cada categoria
        Name = name;
        UserId = userId;
    }

    /// <summary>
    /// Registra a classificação de cortabilidade informada pelo usuário.
    /// </summary>
    /// <param name="nature">Nova natureza do gasto desta categoria.</param>
    public void Reclassify(ExpenseNature nature) => Nature = nature;
}
