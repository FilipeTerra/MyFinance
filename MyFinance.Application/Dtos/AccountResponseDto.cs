using MyFinance.Domain.Enums;
using System;

namespace MyFinance.Application.Dtos;

public class AccountResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Podemos retornar o nome do Enum para o frontend
    public string TypeName { get; set; } = string.Empty;
    public AccountType Type { get; set; }

    public decimal InitialBalance { get; set; }

    /// <summary>
    /// O saldo calculado (InitialBalance + Transações).
    /// Será preenchido pelo Serviço.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    public Guid UserId { get; set; }
}