using System;

namespace MyFinance.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid UserId { get; set; } // A Categoria pertence a UM usuário
    public User User { get; set; } = null!;
}