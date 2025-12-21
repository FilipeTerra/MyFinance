using MyFinance.Application.Dtos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinance.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    // Futuramente, injetaremos ITransactionRepository aqui para calcular o saldo.

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<ServiceResponse<AccountResponseDto>> CreateAccountAsync(AccountRequestDto dto, Guid userId)
    {
        var newAccount = new Account
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = dto.Type,
            InitialBalance = dto.InitialBalance,
            CreatedAt = DateTime.UtcNow,
            UserId = userId // O UserId vem do token (seguro), n�o do DTO
        };

        await _accountRepository.AddAsync(newAccount);
        await _accountRepository.SaveChangesAsync();

        var responseDto = MapAccountToResponseDto(newAccount);
        return new ServiceResponse<AccountResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<IEnumerable<AccountResponseDto>>> GetAllAccountsAsync(Guid userId)
    {
        var accounts = await _accountRepository.GetAllByUserIdAsync(userId);

        // TODO: Futuramente, o CurrentBalance deve ser calculado:
        // InitialBalance + (Soma de Receitas) - (Soma de Despesas)

        var responseDtos = accounts.Select(MapAccountToResponseDto).ToList();
        return new ServiceResponse<IEnumerable<AccountResponseDto>> { Data = responseDtos };
    }

    public async Task<ServiceResponse<AccountResponseDto>> UpdateAccountAsync(Guid accountId, UpdateAccountRequestDto dto, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, userId);

        if (account == null)
        {
            return new ServiceResponse<AccountResponseDto>
            {
                Success = false,
                ErrorMessage = "Conta n�o encontrada ou n�o pertence ao usu�rio."
            };
        }

        // Atualiza os campos permitidos
        account.Name = dto.Name;
        account.Type = dto.Type;

        _accountRepository.Update(account);
        await _accountRepository.SaveChangesAsync();

        var responseDto = MapAccountToResponseDto(account);
        return new ServiceResponse<AccountResponseDto> { Data = responseDto };
    }

    public async Task<ServiceResponse<bool>> DeleteAccountAsync(Guid accountId, Guid userId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, userId);

        if (account == null)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                ErrorMessage = "Conta n�o encontrada ou n�o pertence ao usu�rio."
            };
        }

        // REGRA DE NEG�CIO (AC 4.3): N�o excluir conta com transa��es
        // TODO: Adicionar verifica��o com TransactionRepository quando ele existir
        // if (await _transactionRepository.HasTransactionsAsync(accountId))
        // {
        //    return new ServiceResponse<bool> { Success = false, ErrorMessage = "N�o � poss�vel excluir contas com transa��es." };
        // }

        _accountRepository.Delete(account);
        await _accountRepository.SaveChangesAsync();

        return new ServiceResponse<bool> { Data = true };
    }

    // --- M�todo Auxiliar de Mapeamento ---
    private AccountResponseDto MapAccountToResponseDto(Account account)
    {
        return new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            TypeName = account.Type.ToString(), // Converte Enum para String
            InitialBalance = account.InitialBalance,

            // NOTA: CurrentBalance por enquanto � s� o InitialBalance.
            // Isso ser� atualizado quando tivermos transa��es.
            CurrentBalance = account.InitialBalance,

            UserId = account.UserId
        };
    }
}