using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinance.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository; // Necessário para validar a conta

    public TransactionService(ITransactionRepository transactionRepository, IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
    }

    public async Task<ServiceResponse<TransactionResponseDto>> CreateTransactionAsync(CreateTransactionRequestDto dto, Guid userId)
    {
        // 1. Validar se a conta pertence ao usuário
        var account = await _accountRepository.GetByIdAsync(dto.AccountId, userId);
        if (account == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Conta não encontrada ou não pertence ao usuário." };
        }

        // 2. Criar a entidade Transação
        var newTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Description = dto.Description,
            Amount = dto.Amount, // Valor sempre positivo
            Type = dto.Type,
            Date = dto.Date.ToUniversalTime(), // Armazenar em UTC
            AccountId = dto.AccountId,
            CreatedAt = DateTime.UtcNow
            // CategoryId = dto.CategoryId // (Futuro)
        };

        // 3. Salvar no banco
        await _transactionRepository.AddAsync(newTransaction);
        await _transactionRepository.SaveChangesAsync();

        // 4. Mapear para DTO de resposta (incluindo nome da conta)
        // Precisamos recarregar a transação com a conta para o mapeamento
        var savedTransaction = await _transactionRepository.GetByIdAsync(newTransaction.Id, userId);
        var responseDto = MapTransactionToResponseDto(savedTransaction!); // Usamos ! pois acabamos de criar

        return new ServiceResponse<TransactionResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId)
    {
        // O repositório já valida se a conta pertence ao usuário
        var transactions = await _transactionRepository.GetAllByAccountIdAsync(accountId, userId);

        var responseDtos = transactions.Select(MapTransactionToResponseDto).ToList();

        return new ServiceResponse<IEnumerable<TransactionResponseDto>> { Data = responseDtos };
    }

    public async Task<ServiceResponse<TransactionResponseDto>> GetTransactionByIdAsync(Guid transactionId, Guid userId)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, userId);

        if (transaction == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Transação não encontrada ou não pertence ao usuário." };
        }

        var responseDto = MapTransactionToResponseDto(transaction);
        return new ServiceResponse<TransactionResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<TransactionResponseDto>> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequestDto dto, Guid userId)
    {
        // 1. Buscar a transação existente (já valida o usuário)
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, userId);
        if (transaction == null)
        {
            return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Transação não encontrada ou não pertence ao usuário." };
        }

        // 2. Validar a nova conta (caso tenha sido alterada)
        if (transaction.AccountId != dto.AccountId)
        {
            var newAccount = await _accountRepository.GetByIdAsync(dto.AccountId, userId);
            if (newAccount == null)
            {
                return new ServiceResponse<TransactionResponseDto> { Success = false, ErrorMessage = "Nova conta não encontrada ou não pertence ao usuário." };
            }
        }

        // 3. Atualizar os dados da entidade
        transaction.Description = dto.Description;
        transaction.Amount = dto.Amount;
        transaction.Type = dto.Type;
        transaction.Date = dto.Date.ToUniversalTime();
        transaction.AccountId = dto.AccountId;
        // transaction.CategoryId = dto.CategoryId; // (Futuro)

        // 4. Salvar
        _transactionRepository.Update(transaction);
        await _transactionRepository.SaveChangesAsync();

        // 5. Mapear e retornar (recarregando para pegar a Account atualizada se mudou)
        var updatedTransaction = await _transactionRepository.GetByIdAsync(transaction.Id, userId);
        var responseDto = MapTransactionToResponseDto(updatedTransaction!);

        return new ServiceResponse<TransactionResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<bool>> DeleteTransactionAsync(Guid transactionId, Guid userId)
    {
        // 1. Buscar a transação (já valida o usuário)
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, userId);
        if (transaction == null)
        {
            return new ServiceResponse<bool> { Success = false, ErrorMessage = "Transação não encontrada ou não pertence ao usuário." };
        }

        // 2. Deletar
        _transactionRepository.Delete(transaction);
        await _transactionRepository.SaveChangesAsync();

        return new ServiceResponse<bool> { Data = true };
    }


    // --- Método Auxiliar de Mapeamento ---
    private TransactionResponseDto MapTransactionToResponseDto(Transaction transaction)
    {
        // Assume que transaction.Account foi carregada pelo repositório (.Include)
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
            AccountName = transaction.Account?.Name ?? "Conta não encontrada" // Usa o objeto Account carregado
            // CategoryId = transaction.CategoryId, // (Futuro)
            // CategoryName = transaction.Category?.Name // (Futuro)
        };
    }
}