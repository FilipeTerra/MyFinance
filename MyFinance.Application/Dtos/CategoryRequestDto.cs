using System.ComponentModel.DataAnnotations;

namespace MyFinance.Application.Dtos;

public class CategoryRequestDto
{
    [Required(ErrorMessage = "O Nome da categoria é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O Nome deve ter entre 2 e 100 caracteres.")]
    public string Name { get; set; } = string.Empty;
}