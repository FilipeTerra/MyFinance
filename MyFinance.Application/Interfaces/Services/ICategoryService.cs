using MyFinance.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services;

// Reutiliza a classe ServiceResponse<T> definida em IAccount.Service.cs
public interface ICategoryService
{
    /// <summary>
    /// Cria uma nova categoria para o usu�rio.
    /// </summary>
    Task<ServiceResponse<CategoryResponseDto>> CreateCategoryAsync(CategoryRequestDto dto, Guid userId);

    /// <summary>
    /// Lista todas as categorias do usu�rio.
    /// </summary>
    Task<ServiceResponse<IEnumerable<CategoryResponseDto>>> GetAllCategoriesAsync(Guid userId);

    /// <summary>
    /// Atualiza uma categoria existente do usu�rio.
    /// </summary>
    Task<ServiceResponse<CategoryResponseDto>> UpdateCategoryAsync(Guid categoryId, CategoryRequestDto dto, Guid userId);

    /// <summary>
    /// Exclui uma categoria do usu�rio (se n�o houver transa��es).
    /// </summary>
    Task<ServiceResponse<bool>> DeleteCategoryAsync(Guid categoryId, Guid userId);
}