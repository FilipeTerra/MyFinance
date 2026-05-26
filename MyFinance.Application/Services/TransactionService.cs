using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;
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

    public TransactionService(ITransactionRepository transactionRepository, IAccountRepository accountRepository, ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
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

        // Normalizar o sinal do valor conforme o tipo de transação
        var normalizedAmount = dto.Type == TransactionType.Expense
            ? -Math.Abs(dto.Amount)
            : Math.Abs(dto.Amount);

        // Criar a entidade Transação
        var newTransaction = new Transaction(
            dto.Description,
            normalizedAmount,
            dto.Type,
            dto.Date.ToUniversalTime(), // Armazenar em UTC
            dto.AccountId,
            dto.CategoryId
        );

        account.UpdateBalance(normalizedAmount);

        await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
        try
        {
            await _transactionRepository.AddAsync(newTransaction);
            _accountRepository.Update(account);

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

        transaction.Description = dto.Description;
        transaction.Amount = newNormalizedAmount;
        transaction.Type = dto.Type;
        transaction.Date = dto.Date.ToUniversalTime();
        transaction.AccountId = dto.AccountId;
        transaction.CategoryId = dto.CategoryId;

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

    public async Task SaveBatchAsync(List<SaveBatchTransactionRequestDto> dtos, Guid userId)
    {
        if (dtos == null || !dtos.Any())
        {
            return;
        }

        var newlyCreatedCategories = new Dictionary<string, Guid>();
        var transactionsToSave = new List<Transaction>();

        // Buscar e atualizar o saldo de cada conta uma única vez por AccountId
        var accountsById = new Dictionary<Guid, Account>();

        foreach (var accountGroup in dtos.GroupBy(dto => dto.AccountId))
        {
            var accountId = accountGroup.Key;
            var account = await _accountRepository.GetByIdAsync(accountId, userId);
            if (account == null)
            {
                throw new Exception($"Conta {accountId} não encontrada ou não pertence ao usuário.");
            }

            var groupTotal = accountGroup.Sum(dto => dto.Amount);
            account.UpdateBalance(groupTotal);
            accountsById[accountId] = account;
        }

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
                    finalCategoryId = dto.CategoryId ?? throw new Exception("Categoria não informada para a transação.");
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
            }

            await _transactionRepository.AddRangeAsync(transactionsToSave);

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
    }

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
            AccountName = transaction.Account?.Name ?? "Conta náo encontrada", // Usa o objeto Account carregado
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name ?? "Sem categoria"
        };
    }    
}