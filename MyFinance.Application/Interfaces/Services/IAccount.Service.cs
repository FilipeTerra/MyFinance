using MyFinance.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Interfaces.Services;

/// <summary>
/// Classe genérica para padronizar as respostas dos serviços.
/// </summary>
public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public interface IAccountService
{
    /// <summary>
    /// Cria uma nova conta para o usuário.
    /// </summary>
    Task<ServiceResponse<AccountResponseDto>> CreateAccountAsync(AccountRequestDto dto, Guid userId);

    /// <summary>
    /// Lista todas as contas do usuário.
    /// </summary>
    Task<ServiceResponse<IEnumerable<AccountResponseDto>>> GetAllAccountsAsync(Guid userId);

    /// <summary>
    /// Atualiza uma conta existente do usuário.
    /// </summary>
    Task<ServiceResponse<AccountResponseDto>> UpdateAccountAsync(Guid accountId, UpdateAccountRequestDto dto, Guid userId);

    /// <summary>
    /// Exclui uma conta do usuário.
    /// </summary>
    Task<ServiceResponse<bool>> DeleteAccountAsync(Guid accountId, Guid userId);
}