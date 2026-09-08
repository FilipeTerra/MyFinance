using MyFinance.Application.Dtos;
using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;
using MyFinance.Application.Services.StatementImport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinance.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IFinancialGoalRepository _financialGoalRepository;
    private readonly ICategoryRuleRepository _categoryRuleRepository;

    public TransactionService(ITransactionRepository transactionRepository, IAccountRepository accountRepository, ICategoryRepository categoryRepository, IFinancialGoalRepository financialGoalRepository, ICategoryRuleRepository categoryRuleRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _financialGoalRepository = financialGoalRepository;
        _categoryRuleRepository = categoryRuleRepository;
    }

    public async Task<ServiceResponse<TransactionResponseDto>> CreateTransactionAsync(CreateTransactionRequestDto dto, Guid userId)
    {
        // Validar se a conta pertence ao usuário
        var account = await _accountRepository.GetByIdAsync(dto.AccountId, userId);
        if (account == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Conta náo encontrada ou náo pertence ao usuário." };
        }

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, userId);
        if (category == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Categoria náo encontrada ou náo pertence ao usuário." };
        }

        // Normalizar o sinal do valor: Expense e Investment debitam da conta
        var normalizedAmount = (dto.Type == TransactionType.Expense || dto.Type == TransactionType.Investment)
            ? -Math.Abs(dto.Amount)
            : Math.Abs(dto.Amount);

        // Criar a entidade Transação
        var newTransaction = new Transaction(
            dto.Description,
            normalizedAmount,
            dto.Type,
            dto.Date.ToUniversalTime(),
            dto.AccountId,
            dto.CategoryId,
            dto.FinancialGoalId
        );

        account.UpdateBalance(normalizedAmount);

        // Se for um aporte numa meta, creditar o valor na meta
        FinancialGoal? goal = null;
        if (dto.Type == TransactionType.Investment && dto.FinancialGoalId.HasValue)
        {
            goal = await _financialGoalRepository.GetByIdAsync(dto.FinancialGoalId.Value);
            if (goal == null)
            {
                return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Meta financeira não encontrada." };
            }
            goal.AddContribution(Math.Abs(dto.Amount));
        }

        await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
        try
        {
            await _transactionRepository.AddAsync(newTransaction);
            _accountRepository.Update(account);

            if (goal != null)
                await _financialGoalRepository.UpdateAsync(goal);

            await _transactionRepository.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        // Mapear para DTO de resposta (incluindo nome da conta)
        // Precisamos recarregar a transação com a conta para o mapeamento
        var savedTransaction = await _transactionRepository.GetByIdAsync(newTransaction.Id, userId);
        var responseDto = MapTransactionToResponseDto(savedTransaction!); // Usamos ! pois acabamos de criar

        return new ServiceResponse<TransactionResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId)
    {
        // O repositário já valida se a conta pertence ao usuário
        var transactions = await _transactionRepository.GetAllByAccountIdAsync(accountId, userId);

        var responseDtos = transactions.Select(MapTransactionToResponseDto).ToList();

        return new ServiceResponse<IEnumerable<TransactionResponseDto>> { Data = responseDtos };
    }

    public async Task<ServiceResponse<TransactionResponseDto>> GetTransactionByIdAsync(Guid transactionId, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, userId);

        if (transaction == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Transação náo encontrada ou náo pertence ao usuário." };
        }

        var responseDto = MapTransactionToResponseDto(transaction);
        return new ServiceResponse<TransactionResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<TransactionResponseDto>> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequestDto dto, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, userId);
        if (transaction == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Transação náo encontrada ou náo pertence ao usuário." };
        }

        // Buscar conta original para reverter o efeito do valor antigo
        var oldAccount = await _accountRepository.GetByIdAsync(transaction.AccountId, userId);
        if (oldAccount == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Conta original não encontrada." };
        }

        var oldAmount = transaction.Amount; // já normalizado com sinal
        bool accountChanged = transaction.AccountId != dto.AccountId;

        // Resolver conta destino (pode ser a mesma ou uma nova)
        Account targetAccount;
        if (accountChanged)
        {
            var newAccount = await _accountRepository.GetByIdAsync(dto.AccountId, userId);
            if (newAccount == null)
            {
                return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Nova conta náo encontrada ou náo pertence ao usuário." };
            }
            targetAccount = newAccount;
        }
        else
        {
            targetAccount = oldAccount;
        }

        if (transaction.CategoryId != dto.CategoryId)
        {
            var newCategory = await _categoryRepository.GetByIdAsync(dto.CategoryId, userId);
            if (newCategory == null)
            {
                return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Nova categoria náo encontrada ou náo pertence ao usuário." };
            }
        }

        var newNormalizedAmount = dto.Type == TransactionType.Expense
            ? -Math.Abs(dto.Amount)
            : Math.Abs(dto.Amount);

        // Ajuste de saldo:
        // 1. Desfaz o efeito do valor antigo na conta original
        // 2. Aplica o novo valor na conta destino
        // Ex: despesa(-50) → receita(+50): reverte +50, aplica +50 = saldo +100
        oldAccount.UpdateBalance(-oldAmount);
        targetAccount.UpdateBalance(newNormalizedAmount);

        transaction.Reassign(dto.Description, newNormalizedAmount, dto.Type, dto.Date.ToUniversalTime(), dto.AccountId, dto.CategoryId);

        await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
        try
        {
            _transactionRepository.Update(transaction);
            _accountRepository.Update(oldAccount);
            if (accountChanged)
                _accountRepository.Update(targetAccount);

            await _transactionRepository.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        var updatedTransaction = await _transactionRepository.GetByIdAsync(transaction.Id, userId);
        var responseDto = MapTransactionToResponseDto(updatedTransaction!);

        return new ServiceResponse<TransactionResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<bool>> DeleteTransactionAsync(Guid transactionId, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, userId);
        if (transaction == null)
        {
            return new ServiceResponse<bool> { Success = false, ErrorMessage = "Transação náo encontrada ou náo pertence ao usuário." };
        }

        var account = await _accountRepository.GetByIdAsync(transaction.AccountId, userId);
        if (account == null)
        {
            return new ServiceResponse<bool> { Success = false, ErrorMessage = "Conta associada à transação não encontrada." };
        }

        // Reverter o efeito da transação no saldo (Amount já está normalizado com sinal)
        account.UpdateBalance(-transaction.Amount);

        await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
        try
        {
            _transactionRepository.Delete(transaction);
            _accountRepository.Update(account);

            await _transactionRepository.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    
        return new ServiceResponse<bool> { Data = true };
    }

    public async Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> SearchTransactionsAsync(Guid userId, TransactionSearchRequestDto filters)
    {
        // Chama o novo mátodo do repositário
        var transactions = await _transactionRepository.GetByFilterAsync(userId, filters);

        var responseDtos = transactions.Select(MapTransactionToResponseDto).ToList();

        return new ServiceResponse<IEnumerable<TransactionResponseDto>> { Data = responseDtos };
    }

    public async Task<ServiceResponse<SaveBatchResultDto>> SaveBatchAsync(
        List<SaveBatchTransactionRequestDto> dtos, Guid userId)
    {
        var result = new SaveBatchResultDto();

        if (dtos == null || !dtos.Any())
        {
            return new ServiceResponse<SaveBatchResultDto> { Data = result };
        }

        // A validação roda inteira ANTES de qualquer escrita: assim o usuário
        // recebe todos os problemas de uma vez e o banco não é tocado enquanto
        // houver linha inválida.
        var accountsById = await LoadAccountsAsync(dtos, userId, result.Errors);
        var userCategoryIds = (await _categoryRepository.GetAllByUserIdAsync(userId))
            .Select(c => c.Id)
            .ToHashSet();

        ValidateLines(dtos, accountsById, userCategoryIds, result.Errors);

        if (result.Errors.Count > 0)
        {
            return new ServiceResponse<SaveBatchResultDto>
            {
                Data = result,
                Success = false,
                ErrorMessage = result.Errors.Count == 1
                    ? "Uma transação do lote precisa ser corrigida."
                    : $"{result.Errors.Count} transações do lote precisam ser corrigidas."
            };
        }

        foreach (var accountGroup in dtos.GroupBy(dto => dto.AccountId))
        {
            accountsById[accountGroup.Key].UpdateBalance(accountGroup.Sum(dto => dto.Amount));
        }

        var newlyCreatedCategories = new Dictionary<string, Guid>();
        var transactionsToSave = new List<Transaction>();

        // O que o usuário confirma aqui vira memória para a próxima importação.
        // A chave é a mesma normalização usada na leitura do extrato — se as duas
        // divergirem, o aprendizado nunca é reencontrado.
        var learnedRules = new Dictionary<string, Guid>(StringComparer.Ordinal);

        await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
        try
        {
            foreach (var dto in dtos)
            {
                Guid finalCategoryId;

                // Lógica de Resolução de Categoria
                if (dto.IsNewCategory && !string.IsNullOrWhiteSpace(dto.NewCategoryName))
                {
                    if (newlyCreatedCategories.ContainsKey(dto.NewCategoryName))
                    {
                        finalCategoryId = newlyCreatedCategories[dto.NewCategoryName];
                    }
                    else
                    {
                        var existing = await _categoryRepository.GetByNameAsync(dto.NewCategoryName, userId);
                        if (existing != null)
                        {
                            finalCategoryId = existing.Id;
                        }
                        else
                        {
                            var newCategory = new Category(dto.NewCategoryName, userId);
                            await _categoryRepository.AddAsync(newCategory);
                            finalCategoryId = newCategory.Id;
                            newlyCreatedCategories.Add(dto.NewCategoryName, finalCategoryId);
                        }
                    }
                }
                else
                {
                    // A validação acima já garantiu que o Id existe e é do usuário.
                    finalCategoryId = dto.CategoryId!.Value;
                }

                var transaction = new Transaction(
                    dto.Description,
                    dto.Amount,
                    dto.Amount >= 0 ? TransactionType.Income : TransactionType.Expense,
                    dto.Date,
                    dto.AccountId,
                    finalCategoryId
                );

                transactionsToSave.Add(transaction);

                var descriptionKey = StatementTextNormalizer.Normalize(dto.Description);
                if (descriptionKey.Length > 0)
                {
                    // Descrição repetida no lote: vale a última classificação escolhida.
                    learnedRules[descriptionKey] = finalCategoryId;
                }
            }

            await _transactionRepository.AddRangeAsync(transactionsToSave);

            // Dentro da mesma transação de banco: ou o lote e o aprendizado entram
            // juntos, ou nenhum dos dois entra.
            await _categoryRuleRepository.UpsertRangeAsync(
                userId,
                learnedRules.Select(r => new CategoryRuleDraft(r.Key, r.Value)).ToList());

            foreach (var account in accountsById.Values)
            {
                _accountRepository.Update(account);
            }

            await _transactionRepository.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }

        result.SavedCount = transactionsToSave.Count;
        return new ServiceResponse<SaveBatchResultDto> { Data = result };
    }

    /// <summary>
    /// Carrega as contas citadas no lote. Conta inexistente ou de outro usuário
    /// vira erro nas linhas dela — antes isso era uma exceção genérica, que na
    /// tela virava "erro ao salvar" sem dizer o quê.
    /// </summary>
    private async Task<Dictionary<Guid, Account>> LoadAccountsAsync(
        List<SaveBatchTransactionRequestDto> dtos, Guid userId, List<BatchLineErrorDto> errors)
    {
        var accountsById = new Dictionary<Guid, Account>();

        foreach (var accountId in dtos.Select(d => d.AccountId).Distinct())
        {
            var account = await _accountRepository.GetByIdAsync(accountId, userId);
            if (account != null)
            {
                accountsById[accountId] = account;
                continue;
            }

            AddLineErrors(
                dtos, errors,
                (dto, _) => dto.AccountId == accountId,
                "Conta não encontrada ou não pertence ao usuário.");
        }

        return accountsById;
    }

    private static void ValidateLines(
        List<SaveBatchTransactionRequestDto> dtos,
        Dictionary<Guid, Account> accountsById,
        HashSet<Guid> userCategoryIds,
        List<BatchLineErrorDto> errors)
    {
        for (var index = 0; index < dtos.Count; index++)
        {
            var dto = dtos[index];

            // Conta já reportada em LoadAccountsAsync; não repetir a mesma linha.
            if (!accountsById.ContainsKey(dto.AccountId))
                continue;

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                errors.Add(BuildError(index, dto, "A transação precisa de uma descrição."));
                continue;
            }

            if (dto.Date == default)
            {
                errors.Add(BuildError(index, dto, "A transação precisa de uma data."));
                continue;
            }

            if (dto.IsNewCategory)
            {
                if (string.IsNullOrWhiteSpace(dto.NewCategoryName))
                    errors.Add(BuildError(index, dto, "Informe o nome da nova categoria."));

                continue;
            }

            if (dto.CategoryId is null)
            {
                errors.Add(BuildError(index, dto, "Escolha uma categoria para a transação."));
                continue;
            }

            // Sem esta checagem o Id inválido só falhava no banco, como violação de
            // chave estrangeira — mensagem cifrada e impossível de corrigir na tela.
            if (!userCategoryIds.Contains(dto.CategoryId.Value))
                errors.Add(BuildError(index, dto, "A categoria escolhida não existe mais. Selecione outra."));
        }
    }

    private static void AddLineErrors(
        List<SaveBatchTransactionRequestDto> dtos,
        List<BatchLineErrorDto> errors,
        Func<SaveBatchTransactionRequestDto, int, bool> predicate,
        string message)
    {
        for (var index = 0; index < dtos.Count; index++)
        {
            if (predicate(dtos[index], index))
                errors.Add(BuildError(index, dtos[index], message));
        }
    }

    private static BatchLineErrorDto BuildError(int index, SaveBatchTransactionRequestDto dto, string message) =>
        new() { Index = index, Description = dto.Description ?? string.Empty, Message = message };

    // --- Mátodos Auxiliares ---
    private TransactionResponseDto MapTransactionToResponseDto(Transaction transaction)
    {
        // Assume que transaction.Account foi carregada pelo repositário (.Include)
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Type = transaction.Type,
            TypeName = transaction.Type.ToString(),
            Date = transaction.Date,
            CreatedAt = transaction.CreatedAt,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account?.Name ?? "Conta náo encontrada",
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name ?? "Sem categoria",
            FinancialGoalId = transaction.FinancialGoalId
        };
    }    
}