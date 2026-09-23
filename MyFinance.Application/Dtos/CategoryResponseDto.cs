using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos;

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Classificação de cortabilidade dada pelo usuário; base do plano de corte da sugestão.</summary>
    public ExpenseNature Nature { get; set; }
}