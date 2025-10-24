using MyFinance.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services;

// Reutilizaremos a classe ServiceResponse<T> definida em IAccount.Service.cs
// Se preferir, pode mov�-la para um arquivo pr�prio (ex: ServiceResponse.cs)

public interface ITransactionService
{
    /// <summary>
    /// Cria uma nova transa��o para uma conta espec�fica do usu�rio.
    /// </summary>
    Task<ServiceResponse<TransactionResponseDto>> CreateTransactionAsync(CreateTransactionRequestDto dto, Guid userId);

    /// <summary>
    /// Lista todas as transa��es de uma conta espec�fica do usu�rio.
    /// </summary>
    Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId);

    /// <summary>
    /// Busca uma transa��o espec�fica pelo ID, garantindo que perten�a ao usu�rio.
    /// </summary>
    Task<ServiceResponse<TransactionResponseDto>> GetTransactionByIdAsync(Guid transactionId, Guid userId);

    /// <summary>
    /// Atualiza uma transa��o existente.
    /// </summary>
    Task<ServiceResponse<TransactionResponseDto>> UpdateTransactionAsync(Guid transactionId, UpdateTransactionRequestDto dto, Guid userId);

    /// <summary>
    /// Exclui uma transa��o.
    /// </summary>
    Task<ServiceResponse<bool>> DeleteTransactionAsync(Guid transactionId, Guid userId);

    /// <summary>
    /// Busca transa��es de forma din�mica com base nos filtros.
    /// </summary>
    Task<ServiceResponse<IEnumerable<TransactionResponseDto>>> SearchTransactionsAsync(Guid userId, TransactionSearchRequestDto filters);
}