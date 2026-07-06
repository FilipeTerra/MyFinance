using Moq;
using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Services;
using MyFinance.Application.Tests.TestHelpers;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IFinancialGoalRepository> _goalRepository = new();
    private readonly TransactionService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TransactionServiceTests()
    {
        _sut = new TransactionService(
            _transactionRepository.Object,
            _accountRepository.Object,
            _categoryRepository.Object,
            _goalRepository.Object);
        _transactionRepository.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(MockDbTransaction.Create().Object);
    }

    private Account BuildAccount(decimal initial = 1000m) => new("Conta", AccountType.ContaCorrente, initial, _userId);
    private Category BuildCategory() => new("Categoria", _userId);

    // ---------- CreateTransactionAsync ----------

    [Fact]
    public async Task CreateTransactionAsync_Expense_DebitsAccountAndPersists()
    {
        var account = BuildAccount(1000m);
        var category = BuildCategory();
        var dto = new CreateTransactionRequestDto
        {
            Description = "Almoço",
            Amount = 50m,
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow,
            AccountId = account.Id,
            CategoryId = category.Id
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId))
            .ReturnsAsync(new Transaction("Almoço", -50m, TransactionType.Expense, dto.Date, account.Id, category.Id));

        var result = await _sut.CreateTransactionAsync(dto, _userId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(950m, account.Balance); // 1000 - 50
        _transactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Amount == -50m)), Times.Once);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_Income_CreditsAccount()
    {
        var account = BuildAccount(1000m);
        var category = BuildCategory();
        var dto = new CreateTransactionRequestDto
        {
            Description = "Salário",
            Amount = 500m,
            Type = TransactionType.Income,
            Date = DateTime.UtcNow,
            AccountId = account.Id,
            CategoryId = category.Id
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId))
            .ReturnsAsync(new Transaction("Salário", 500m, TransactionType.Income, dto.Date, account.Id, category.Id));

        var result = await _sut.CreateTransactionAsync(dto, _userId);

        Assert.True(result.Success);
        Assert.Equal(1500m, account.Balance);
        _transactionRepository.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Amount == 500m)), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenAccountNotFound_ReturnsFailure()
    {
        _accountRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Account?)null);
        var dto = new CreateTransactionRequestDto { AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Amount = 10m };

        var result = await _sut.CreateTransactionAsync(dto, _userId);

        Assert.False(result.Success);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenCategoryNotFound_ReturnsFailure()
    {
        var account = BuildAccount();
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Category?)null);
        var dto = new CreateTransactionRequestDto { AccountId = account.Id, CategoryId = Guid.NewGuid(), Amount = 10m };

        var result = await _sut.CreateTransactionAsync(dto, _userId);

        Assert.False(result.Success);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task CreateTransactionAsync_InvestmentWithGoal_AddsContributionToGoal()
    {
        var account = BuildAccount(1000m);
        var category = BuildCategory();
        var goal = new FinancialGoal(_userId, "Meta", 5000m, DateTime.UtcNow);
        var dto = new CreateTransactionRequestDto
        {
            Description = "Aporte",
            Amount = 200m,
            Type = TransactionType.Investment,
            Date = DateTime.UtcNow,
            AccountId = account.Id,
            CategoryId = category.Id,
            FinancialGoalId = goal.Id
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        _goalRepository.Setup(r => r.GetByIdAsync(goal.Id)).ReturnsAsync(goal);
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId))
            .ReturnsAsync(new Transaction("Aporte", -200m, TransactionType.Investment, dto.Date, account.Id, category.Id, goal.Id));

        var result = await _sut.CreateTransactionAsync(dto, _userId);

        Assert.True(result.Success);
        Assert.Equal(800m, account.Balance);      // débito de 200
        Assert.Equal(200m, goal.CurrentAmount);   // aporte creditado na meta
        _goalRepository.Verify(r => r.UpdateAsync(goal), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_InvestmentWithMissingGoal_ReturnsFailure()
    {
        var account = BuildAccount();
        var category = BuildCategory();
        var dto = new CreateTransactionRequestDto
        {
            Description = "Aporte",
            Amount = 200m,
            Type = TransactionType.Investment,
            Date = DateTime.UtcNow,
            AccountId = account.Id,
            CategoryId = category.Id,
            FinancialGoalId = Guid.NewGuid()
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByIdAsync(category.Id, _userId)).ReturnsAsync(category);
        _goalRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinancialGoal)null!);

        var result = await _sut.CreateTransactionAsync(dto, _userId);

        Assert.False(result.Success);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ---------- GetTransactionByIdAsync ----------

    [Fact]
    public async Task GetTransactionByIdAsync_WhenFound_ReturnsDto()
    {
        var account = BuildAccount();
        var category = BuildCategory();
        var transaction = new Transaction("Compra", -30m, TransactionType.Expense, DateTime.UtcNow, account.Id, category.Id);
        _transactionRepository.Setup(r => r.GetByIdAsync(transaction.Id, _userId)).ReturnsAsync(transaction);

        var result = await _sut.GetTransactionByIdAsync(transaction.Id, _userId);

        Assert.True(result.Success);
        Assert.Equal(transaction.Id, result.Data!.Id);
        Assert.Equal(-30m, result.Data.Amount);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WhenNotFound_ReturnsFailure()
    {
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Transaction?)null);

        var result = await _sut.GetTransactionByIdAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        Assert.Null(result.Data);
    }

    // ---------- GetTransactionsByAccountIdAsync ----------

    [Fact]
    public async Task GetTransactionsByAccountIdAsync_ReturnsMappedList()
    {
        var accountId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new("A", -10m, TransactionType.Expense, DateTime.UtcNow, accountId, catId),
            new("B", 20m, TransactionType.Income, DateTime.UtcNow, accountId, catId)
        };
        _transactionRepository.Setup(r => r.GetAllByAccountIdAsync(accountId, _userId)).ReturnsAsync(transactions);

        var result = await _sut.GetTransactionsByAccountIdAsync(accountId, _userId);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count());
    }

    // ---------- UpdateTransactionAsync ----------

    [Fact]
    public async Task UpdateTransactionAsync_SameAccount_AdjustsBalanceAndReassigns()
    {
        var account = BuildAccount(1000m);
        var category = BuildCategory();
        // transação existente: despesa de -100 (saldo já reflete isso conceitualmente)
        var existing = new Transaction("Antiga", -100m, TransactionType.Expense, DateTime.UtcNow, account.Id, category.Id);

        _transactionRepository.Setup(r => r.GetByIdAsync(existing.Id, _userId)).ReturnsAsync(existing);
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);

        var dto = new UpdateTransactionRequestDto
        {
            Description = "Atualizada",
            Amount = 40m,
            Type = TransactionType.Income, // vira receita +40
            Date = DateTime.UtcNow,
            AccountId = account.Id,
            CategoryId = category.Id
        };

        var result = await _sut.UpdateTransactionAsync(existing.Id, dto, _userId);

        Assert.True(result.Success);
        // reverte -(-100)=+100 e aplica +40  => 1000 + 100 + 40 = 1140
        Assert.Equal(1140m, account.Balance);
        Assert.Equal("Atualizada", existing.Description);
        Assert.Equal(40m, existing.Amount);
        Assert.Equal(TransactionType.Income, existing.Type);
        _transactionRepository.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WhenTransactionNotFound_ReturnsFailure()
    {
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Transaction?)null);
        var dto = new UpdateTransactionRequestDto { AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Amount = 10m };

        var result = await _sut.UpdateTransactionAsync(Guid.NewGuid(), dto, _userId);

        Assert.False(result.Success);
        _transactionRepository.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WhenOriginalAccountNotFound_ReturnsFailure()
    {
        var category = BuildCategory();
        var accountId = Guid.NewGuid();
        var existing = new Transaction("Antiga", -100m, TransactionType.Expense, DateTime.UtcNow, accountId, category.Id);
        _transactionRepository.Setup(r => r.GetByIdAsync(existing.Id, _userId)).ReturnsAsync(existing);
        _accountRepository.Setup(r => r.GetByIdAsync(accountId, _userId)).ReturnsAsync((Account?)null);

        var dto = new UpdateTransactionRequestDto { AccountId = accountId, CategoryId = category.Id, Amount = 10m, Type = TransactionType.Expense, Date = DateTime.UtcNow, Description = "X" };

        var result = await _sut.UpdateTransactionAsync(existing.Id, dto, _userId);

        Assert.False(result.Success);
    }

    // ---------- DeleteTransactionAsync ----------

    [Fact]
    public async Task DeleteTransactionAsync_WhenFound_ReversesBalanceAndDeletes()
    {
        var account = BuildAccount(1000m);
        var category = BuildCategory();
        var transaction = new Transaction("Despesa", -100m, TransactionType.Expense, DateTime.UtcNow, account.Id, category.Id);

        _transactionRepository.Setup(r => r.GetByIdAsync(transaction.Id, _userId)).ReturnsAsync(transaction);
        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);

        var result = await _sut.DeleteTransactionAsync(transaction.Id, _userId);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal(1100m, account.Balance); // reverte o débito de -100
        _transactionRepository.Verify(r => r.Delete(transaction), Times.Once);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WhenNotFound_ReturnsFailure()
    {
        _transactionRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Transaction?)null);

        var result = await _sut.DeleteTransactionAsync(Guid.NewGuid(), _userId);

        Assert.False(result.Success);
        _transactionRepository.Verify(r => r.Delete(It.IsAny<Transaction>()), Times.Never);
    }

    // ---------- SearchTransactionsAsync ----------

    [Fact]
    public async Task SearchTransactionsAsync_ReturnsFilteredResults()
    {
        var accountId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var filters = new TransactionSearchRequestDto();
        var transactions = new List<Transaction>
        {
            new("Filtrada", -10m, TransactionType.Expense, DateTime.UtcNow, accountId, catId)
        };
        _transactionRepository.Setup(r => r.GetByFilterAsync(_userId, filters)).ReturnsAsync(transactions);

        var result = await _sut.SearchTransactionsAsync(_userId, filters);

        Assert.True(result.Success);
        Assert.Single(result.Data!);
    }

    // ---------- SaveBatchAsync ----------

    [Fact]
    public async Task SaveBatchAsync_WithExistingCategory_UpdatesAccountBalanceAndAddsRange()
    {
        var account = BuildAccount(1000m);
        var categoryId = Guid.NewGuid();
        var dtos = new List<SaveBatchTransactionRequestDto>
        {
            new() { Description = "T1", Amount = -50m, Date = DateTime.UtcNow, AccountId = account.Id, CategoryId = categoryId, IsNewCategory = false },
            new() { Description = "T2", Amount = 200m, Date = DateTime.UtcNow, AccountId = account.Id, CategoryId = categoryId, IsNewCategory = false }
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);

        await _sut.SaveBatchAsync(dtos, _userId);

        Assert.Equal(1150m, account.Balance); // 1000 - 50 + 200
        _transactionRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Transaction>>(t => t.Count() == 2)), Times.Once);
        _transactionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SaveBatchAsync_WithNewCategory_CreatesCategoryOnce()
    {
        var account = BuildAccount(1000m);
        var dtos = new List<SaveBatchTransactionRequestDto>
        {
            new() { Description = "T1", Amount = -50m, Date = DateTime.UtcNow, AccountId = account.Id, IsNewCategory = true, NewCategoryName = "Mercado" },
            new() { Description = "T2", Amount = -20m, Date = DateTime.UtcNow, AccountId = account.Id, IsNewCategory = true, NewCategoryName = "Mercado" }
        };

        _accountRepository.Setup(r => r.GetByIdAsync(account.Id, _userId)).ReturnsAsync(account);
        _categoryRepository.Setup(r => r.GetByNameAsync("Mercado", _userId)).ReturnsAsync((Category?)null);

        await _sut.SaveBatchAsync(dtos, _userId);

        // Mesma categoria nova reutilizada entre as duas transações -> criada só uma vez
        _categoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
        _transactionRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<Transaction>>(t => t.Count() == 2)), Times.Once);
    }

    [Fact]
    public async Task SaveBatchAsync_WhenAccountNotFound_Throws()
    {
        _accountRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Account?)null);
        var dtos = new List<SaveBatchTransactionRequestDto>
        {
            new() { Description = "T1", Amount = -50m, Date = DateTime.UtcNow, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid() }
        };

        await Assert.ThrowsAsync<Exception>(() => _sut.SaveBatchAsync(dtos, _userId));
    }

    [Fact]
    public async Task SaveBatchAsync_WithEmptyList_DoesNothing()
    {
        await _sut.SaveBatchAsync(new List<SaveBatchTransactionRequestDto>(), _userId);

        _transactionRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>()), Times.Never);
    }
}
