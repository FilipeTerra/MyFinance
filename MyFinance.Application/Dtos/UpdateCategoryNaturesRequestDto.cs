using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos;

/// <summary>
/// Classificação de cortabilidade de várias categorias de uma vez. O usuário classifica
/// em bloco na tela de sugestão, então a gravação também é em bloco.
/// </summary>
public class UpdateCategoryNaturesRequestDto
{
    [Required(ErrorMessage = "Informe ao menos uma categoria para classificar.")]
    [MinLength(1, ErrorMessage = "Informe ao menos uma categoria para classificar.")]
    public List<CategoryNatureItemDto> Items { get; set; } = new();
}

/// <summary>Uma categoria e a natureza escolhida pelo usuário.</summary>
public class CategoryNatureItemDto
{
    public Guid CategoryId { get; set; }

    [EnumDataType(typeof(ExpenseNature), ErrorMessage = "Classificação de gasto inválida.")]
    public ExpenseNature Nature { get; set; }
}
