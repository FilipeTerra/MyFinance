using Moq;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Application.Tests.TestHelpers;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class InvestimentoServiceTests
{
    private readonly Mock<IInvestimentoRepository> _investimentoRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly InvestimentoService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public InvestimentoServiceTests()
    {
        _sut = new InvestimentoService(
            _investimentoRepository.Object,
            _transactionRepository.Object,
            _accountRepository.Object,
            _categoryRepository.Object);
        _transactionRepository.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(MockDbTransaction.Create().Object);
    }

    private Account BuildAccount(decimal initial) => new("Conta", AccountType.ContaCorrente, initial, _userId);
    private Category BuildCategory() => new("Investimentos", _userId);

    // ---------- CreateInvestimentoAsync ----------

    [Fact]
    public async Task CreateInvestimentoAsync_WithSufficientBalance_DebitsAccountAndPersistsInvestimentoAndOriginTransaction()
    {
        var account = BuildAccount(1000m);
        var category = BuildCategory();
        var request = new CreateInvestimentoRequestDto
        {
            Nome = "Tesouro Selic",
            ValorInicial = 300m,
            Tipo = InvestmentType.RendaFixa,
            AccountId = account.Id,
            CategoryId = category.Id
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);

        var result = await _sut.CreateInvestimentoAsync(_userId, request);

        Assert.Equal("Tesouro Selic", result.Nome);
        Assert.Equal(300m, result.ValorInicial);
        Assert.Equal(300m, result.ValorAtual);
        Assert.Equal(700m, account.Balance); // 1000 - 300
        _investimentoRepository.Verify(r => r.AddAsync(It.IsAny<Investimento>()), Times.Once);
        _transactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Amount == -300m && t.InvestimentoId != null)), Times.Once);
        _accountRepository.Verify(r => r.Update(account), Times.Once);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateInvestimentoAsync_WhenAccountNotFound_ThrowsInvalidOperation()
    {
        _accountRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Account?)null);
        var request = new CreateInvestimentoRequestDto { Nome = "X", ValorInicial = 100m, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid() };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateInvestimentoAsync(_userId, request));
        _investimentoRepository.Verify(r => r.AddAsync(It.IsAny<Investimento>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvestimentoAsync_WhenCategoryNotFound_ThrowsInvalidOperation()
    {
        var account = BuildAccount(1000m);
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Category?)null);
        var request = new CreateInvestimentoRequestDto { Nome = "X", ValorInicial = 100m, AccountId = account.Id, CategoryId = Guid.NewGuid() };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateInvestimentoAsync(_userId, request));
    }

    [Fact]
    public async Task CreateInvestimentoAsync_WithInsufficientBalance_ThrowsInvalidOperationAndDoesNotPersist()
    {
        var account = BuildAccount(100m);
        var category = BuildCategory();
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        var request = new CreateInvestimentoRequestDto { Nome = "X", ValorInicial = 500m, AccountId = account.Id, CategoryId = category.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateInvestimentoAsync(_userId, request));
        Assert.Equal(100m, account.Balance); // inalterado
        _investimentoRepository.Verify(r => r.AddAsync(It.IsAny<Investimento>()), Times.Never);
    }

    // ---------- GetUserInvestimentosAsync ----------

    [Fact]
    public async Task GetUserInvestimentosAsync_ReturnsMappedList()
    {
        var investimentos = new List<Investimento>
        {
            new(_userId, "PETR4", 100m, InvestmentType.Acao),
            new(_userId, "HGLG11", 200m, InvestmentType.FII)
        };
        _investimentoRepository.Setup(r => r.GetAllByUserIdAsync(_userId)).ReturnsAsync(investimentos);

        var result = (await _sut.GetUserInvestimentosAsync(_userId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Nome == "PETR4");
    }

    // ---------- UpdateValorAtualAsync ----------

    [Fact]
    public async Task UpdateValorAtualAsync_WhenValid_UpdatesValueAndPersists()
    {
        var investimento = new Investimento(_userId, "PETR4", 100m, InvestmentType.Acao);
        _investimentoRepository.Setup(r => r.GetByIdAsync(investimento.Id)).ReturnsAsync(investimento);

        var result = await _sut.UpdateValorAtualAsync(investimento.Id, _userId, 150m);

        Assert.Equal(150m, result.ValorAtual);
        Assert.Equal(50m, result.RentabilidadePercentual);
        _investimentoRepository.Verify(r => r.Update(investimento), Times.Once);
        _investimentoRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateValorAtualAsync_WhenNotFound_ThrowsUnauthorized()
    {
        _investimentoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Investimento)null!);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.UpdateValorAtualAsync(Guid.NewGuid(), _userId, 150m));
    }

    [Fact]
    public async Task UpdateValorAtualAsync_WhenBelongsToAnotherUser_ThrowsUnauthorized()
    {
        var investimento = new Investimento(Guid.NewGuid(), "PETR4", 100m, InvestmentType.Acao);
        _investimentoRepository.Setup(r => r.GetByIdAsync(investimento.Id)).ReturnsAsync(investimento);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.UpdateValorAtualAsync(investimento.Id, _userId, 150m));
        _investimentoRepository.Verify(r => r.Update(It.IsAny<Investimento>()), Times.Never);
    }

    // ---------- DeleteInvestimentoAsync ----------

    [Fact]
    public async Task DeleteInvestimentoAsync_WhenValid_RestoresBalanceRemovesAportesAndDeletes()
    {
        var investimento = new Investimento(_userId, "PETR4", 300m, InvestmentType.Acao);
        var account = BuildAccount(700m); // já debitado do aporte de 300
        var aporte = new Transaction("Aporte", -300m, TransactionType.Investment, DateTime.UtcNow, account.Id, Guid.NewGuid(), null, investimento.Id);

        _investimentoRepository.Setup(r => r.GetByIdAsync(investimento.Id)).ReturnsAsync(investimento);
        _transactionRepository.Setup(r => r.GetByInvestimentoIdAsync(investimento.Id)).ReturnsAsync(new List<Transaction> { aporte });
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);

        await _sut.DeleteInvestimentoAsync(investimento.Id, _userId);

        Assert.Equal(1000m, account.Balance); // 700 + 300 restaurado
        _accountRepository.Verify(r => r.Update(account), Times.Once);
        _transactionRepository.Verify(r => r.Delete(aporte), Times.Once);
        _investimentoRepository.Verify(r => r.Delete(investimento), Times.Once);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteInvestimentoAsync_WhenNotFound_ThrowsUnauthorized()
    {
        _investimentoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Investimento)null!);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.DeleteInvestimentoAsync(Guid.NewGuid(), _userId));
    }

    [Fact]
    public async Task DeleteInvestimentoAsync_WhenBelongsToAnotherUser_ThrowsUnauthorized()
    {
        var investimento = new Investimento(Guid.NewGuid(), "PETR4", 300m, InvestmentType.Acao);
        _investimentoRepository.Setup(r => r.GetByIdAsync(investimento.Id)).ReturnsAsync(investimento);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.DeleteInvestimentoAsync(investimento.Id, _userId));
        _investimentoRepository.Verify(r => r.Delete(It.IsAny<Investimento>()), Times.Never);
    }
}
