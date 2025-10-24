using MyFinance.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services;

// Reutilizaremos a classe ServiceResponse<T> definida em IAccount.Service.cs
// Se preferir, pode movê-la para um arquivo próprio (ex: ServiceResponse.cs)

public interface ITransactionService
{
    /// <summary>
    /// Cria uma nova transação para uma conta específica do usuário.
    /// </summary>
    Task<ServiceResponse<TransactionResponseDto>> CreateTransactionAsync(CreateTransactionRequestDto dto, Guid userId);

    /// <summary>
    /// Lista todas as transações de uma conta específica do usuário.
    /// </summary>
    Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId);

    /// <summary>
    /// Busca uma transação específica pelo ID, garantindo que pertença ao usuário.
    /// </summary>
    Task<ServiceResponse<TransactionResponseDto>> GetTransactionByIdAsync(Guid transactionId, Guid userId);

    /// <summary>
    /// Atualiza uma transação existente.
    /// </summary>
    Task<ServiceResponse<TransactionResponseDto>> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequestDto dto, Guid userId);

    /// <summary>
    /// Exclui uma transação.
    /// </summary>
    Task<ServiceResponse<bool>> DeleteTransactionAsync(Guid transactionId, Guid userId);

    /// <summary>
    /// Busca transações de forma dinâmica com base nos filtros.
    /// </summary>
    Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> SearchTransactionsAsync(Guid userId, TransactionSearchRequestDto filters);
}