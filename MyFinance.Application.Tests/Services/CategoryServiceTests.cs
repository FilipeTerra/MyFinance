using Moq;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly CategoryService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categoryRepository.Object);
    }

    // ---------- CreateCategoryAsync ----------

    [Fact]
    public async Task CreateCategoryAsync_PersistsCategoryAndReturnsDto()
    {
        var dto = new CategoryRequestDto { Name = "Alimentação" };
        Category? saved = null;
        _categoryRepository.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .Callback<Category>(c => saved = c)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateCategoryAsync(dto, _userId);

        Assert.True(result.Success);
        Assert.Equal("Alimentação", result.Data!.Name);
        Assert.NotNull(saved);
        Assert.Equal(_userId, saved!.UserId);
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ---------- GetAllCategoriesAsync ----------

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsMappedCategories()
    {
        var categories = new List<Category>
        {
            new("Cat A", _userId),
            new("Cat B", _userId)
        };
        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(categories);

        var result = await _sut.GetAllCategoriesAsync(_userId);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count());
        Assert.Contains(result.Data!, c => c.Name == "Cat A");
        Assert.Contains(result.Data!, c => c.Name == "Cat B");
    }

    // ---------- UpdateCategoryAsync ----------

    [Fact]
    public async Task UpdateCategoryAsync_WhenExists_UpdatesNameAndPersists()
    {
        var category = new Category("Nome Antigo", _userId);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        var dto = new CategoryRequestDto { Name = "Nome Novo" };

        var result = await _sut.UpdateCategoryAsync(category.Id, dto, _userId);

        Assert.True(result.Success);
        Assert.Equal("Nome Novo", result.Data!.Name);
        Assert.Equal("Nome Novo", category.Name);
        _categoryRepository.Verify(r => r.Update(category), Times.Once);
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenNotFound_ReturnsFailure()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.UpdateCategoryAsync(Guid.NewGuid(), new CategoryRequestDto { Name = "X" }, _userId);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        _categoryRepository.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
    }

    // ---------- DeleteCategoryAsync ----------

    [Fact]
    public async Task DeleteCategoryAsync_WhenExistsAndNoTransactions_Deletes()
    {
        var category = new Category("Cat", _userId);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.HasTransactionsAsync(category.Id)).ReturnsAsync(false);

        var result = await _sut.DeleteCategoryAsync(category.Id, _userId);

        Assert.True(result.Success);
        Assert.True(result.Data);
        _categoryRepository.Verify(r => r.Delete(category), Times.Once);
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenNotFound_ReturnsFailure()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.DeleteCategoryAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        _categoryRepository.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenHasTransactions_ReturnsFailureAndDoesNotDelete()
    {
        var category = new Category("Cat", _userId);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.HasTransactionsAsync(category.Id)).ReturnsAsync(true);

        var result = await _sut.DeleteCategoryAsync(category.Id, _userId);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        _categoryRepository.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
    }
}
