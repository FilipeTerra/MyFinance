using Moq;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

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

    // ---------- UpdateCategoryNaturesAsync ----------

    private static UpdateCategoryNaturesRequestDto Lote(params (Guid Id, ExpenseNature Nature)[] itens) =>
        new()
        {
            Items = itens.Select(i => new CategoryNatureItemDto { CategoryId = i.Id, Nature = i.Nature }).ToList()
        };

    [Fact]
    public async Task UpdateCategoryNaturesAsync_ClassifiesEveryCategoryInTheBatch()
    {
        var lazer = new Category("Lazer", _userId);
        var aluguel = new Category("Aluguel", _userId);
        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { lazer, aluguel });

        var result = await _sut.UpdateCategoryNaturesAsync(
            Lote((lazer.Id, ExpenseNature.Discricionario), (aluguel.Id, ExpenseNature.Essencial)), _userId);

        Assert.True(result.Success);
        Assert.Equal(ExpenseNature.Discricionario, lazer.Nature);
        Assert.Equal(ExpenseNature.Essencial, aluguel.Nature);
        Assert.Equal(2, result.Data!.Count());
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryNaturesAsync_WithCategoryFromAnotherUser_SavesNothing()
    {
        var minha = new Category("Lazer", _userId);
        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { minha });
        var deOutroUsuario = Guid.NewGuid();

        var result = await _sut.UpdateCategoryNaturesAsync(
            Lote((minha.Id, ExpenseNature.Discricionario), (deOutroUsuario, ExpenseNature.Essencial)), _userId);

        // Tudo-ou-nada: o item válido também não é gravado, e o erro aponta qual id reprovou.
        Assert.False(result.Success);
        Assert.Contains(deOutroUsuario.ToString(), result.ErrorMessage);
        Assert.Equal(ExpenseNature.NaoClassificado, minha.Nature);
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        _categoryRepository.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryNaturesAsync_WithDuplicatedCategory_RejectsBatch()
    {
        var lazer = new Category("Lazer", _userId);
        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { lazer });

        var result = await _sut.UpdateCategoryNaturesAsync(
            Lote((lazer.Id, ExpenseNature.Discricionario), (lazer.Id, ExpenseNature.Essencial)), _userId);

        Assert.False(result.Success);
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryNaturesAsync_WithUndefinedNature_RejectsBatch()
    {
        var lazer = new Category("Lazer", _userId);
        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { lazer });

        var result = await _sut.UpdateCategoryNaturesAsync(Lote((lazer.Id, (ExpenseNature)99)), _userId);

        Assert.False(result.Success);
        _categoryRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryNaturesAsync_WithEmptyBatch_Fails()
    {
        var result = await _sut.UpdateCategoryNaturesAsync(Lote(), _userId);

        Assert.False(result.Success);
        _categoryRepository.Verify(r => r.GetAllByUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryNaturesAsync_ReadsCategoriesOnceForTheWholeBatch()
    {
        var a = new Category("A", _userId);
        var b = new Category("B", _userId);
        var c = new Category("C", _userId);
        _categoryRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(new[] { a, b, c });

        await _sut.UpdateCategoryNaturesAsync(
            Lote((a.Id, ExpenseNature.Essencial), (b.Id, ExpenseNature.Discricionario), (c.Id, ExpenseNature.Essencial)),
            _userId);

        // Uma leitura para o lote inteiro, não uma por item.
        _categoryRepository.Verify(r => r.GetAllByUserIdAsync(_userId), Times.Once);
    }
}
