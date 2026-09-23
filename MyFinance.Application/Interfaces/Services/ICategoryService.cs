using MyFinance.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services;

// Reutiliza a classe ServiceResponse<T> definida em IAccount.Service.cs
public interface ICategoryService
{
    /// <summary>
    /// Cria uma nova categoria para o usuário.
    /// </summary>
    Task<ServiceResponse<CategoryResponseDto>> CreateCategoryAsync(CategoryRequestDto dto, Guid userId);

    /// <summary>
    /// Lista todas as categorias do usuário.
    /// </summary>
    Task<ServiceResponse<IEnumerable<CategoryResponseDto>>> GetAllCategoriesAsync(Guid userId);

    /// <summary>
    /// Atualiza uma categoria existente do usuário.
    /// </summary>
    Task<ServiceResponse<CategoryResponseDto>> UpdateCategoryAsync(Guid categoryId, CategoryRequestDto dto, Guid userId);

    /// <summary>
    /// Exclui uma categoria do usuário (se não houver transações).
    /// </summary>
    Task<ServiceResponse<bool>> DeleteCategoryAsync(Guid categoryId, Guid userId);

    /// <summary>
    /// Classifica várias categorias de uma vez como essenciais ou discricionárias.
    /// Operação tudo-ou-nada: o lote inteiro é validado antes de qualquer gravação, e
    /// um id inexistente ou de outro usuário reprova o lote sem alterar nada.
    /// </summary>
    /// <param name="dto">Pares categoria/natureza escolhidos pelo usuário.</param>
    /// <param name="userId">Usuário autenticado, dono das categorias.</param>
    Task<ServiceResponse<IEnumerable<CategoryResponseDto>>> UpdateCategoryNaturesAsync(
        UpdateCategoryNaturesRequestDto dto, Guid userId);
}