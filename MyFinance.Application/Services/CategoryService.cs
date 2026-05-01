using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinance.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResponse<CategoryResponseDto>> CreateCategoryAsync(CategoryRequestDto dto, Guid userId)
    {
        var newCategory = new Category(dto.Name, userId);

        await _categoryRepository.AddAsync(newCategory);
        await _categoryRepository.SaveChangesAsync();

        var responseDto = MapCategoryToResponseDto(newCategory);
        return new ServiceResponse<CategoryResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<IEnumerable<CategoryResponseDto>>> GetAllCategoriesAsync(Guid userId)
    {
        var categories = await _categoryRepository.GetAllByUserIdAsync(userId);
        var responseDtos = categories.Select(MapCategoryToResponseDto).ToList();
        return new ServiceResponse<IEnumerable<CategoryResponseDto>> { Data = responseDtos };
    }

    public async Task<ServiceResponse<CategoryResponseDto>> UpdateCategoryAsync(Guid categoryId, CategoryRequestDto dto, Guid userId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, userId);

        if (category == null)
        {
            return new ServiceResponse<CategoryResponseDto>
            {
                Success = false,
                ErrorMessage = "Categoria n�o encontrada ou n�o pertence ao usu�rio."
            };
        }

        category.Name = dto.Name;
        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync();

        var responseDto = MapCategoryToResponseDto(category);
        return new ServiceResponse<CategoryResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<bool>> DeleteCategoryAsync(Guid categoryId, Guid userId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, userId);

        if (category == null)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                ErrorMessage = "Categoria n�o encontrada ou n�o pertence ao usu�rio."
            };
        }

        // REGRA DE NEG�CIO: N�o excluir categoria com transa��es
        if (await _categoryRepository.HasTransactionsAsync(categoryId))
        {
            return new ServiceResponse<bool> { Success = false, ErrorMessage = "N�o � poss�vel excluir categorias com transa��es associadas." };
        }

        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync();

        return new ServiceResponse<bool> { Data = true };
    }

    private CategoryResponseDto MapCategoryToResponseDto(Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }
}