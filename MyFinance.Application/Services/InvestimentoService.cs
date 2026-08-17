using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Services
{
    public class InvestimentoService : IInvestimentoService
    {
        private readonly IInvestimentoRepository _investimentoRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICotacaoHistoricoRepository _cotacaoHistoricoRepository;

        public InvestimentoService(
            IInvestimentoRepository investimentoRepository,
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository,
            ICategoryRepository categoryRepository,
            ICotacaoHistoricoRepository cotacaoHistoricoRepository)
        {
            _investimentoRepository = investimentoRepository;
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
            _cotacaoHistoricoRepository = cotacaoHistoricoRepository;
        }

        public async Task<InvestimentoResponseDto> CreateInvestimentoAsync(Guid userId, CreateInvestimentoRequestDto request)
        {
            // O dinheiro do investimento precisa ter uma origem: uma conta bancária.
            var account = await _accountRepository.GetByIdAsync(request.AccountId, userId);
            if (account == null)
                throw new InvalidOperationException("Conta de origem não encontrada ou não pertence ao usuário.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, userId);
            if (category == null)
                throw new InvalidOperationException("Categoria não encontrada ou não pertence ao usuário.");

            if (account.Balance < request.ValorInicial)
                throw new InvalidOperationException(
                    $"Saldo insuficiente na conta \"{account.Name}\". Disponível: {account.Balance:C}.");

            // Cria o investimento e a transação de origem (aporte) que debita a conta.
            var investimento = new Investimento(userId, request.Nome, request.ValorInicial, request.Tipo, request.Ticker);

            var normalizedAmount = -Math.Abs(request.ValorInicial); // Investment debita da conta
            var originTransaction = new Transaction(
                description: $"Aporte em investimento: {investimento.Nome}",
                amount: normalizedAmount,
                type: TransactionType.Investment,
                date: DateTime.UtcNow,
                accountId: request.AccountId,
                categoryId: request.CategoryId,
                financialGoalId: null,
                investimentoId: investimento.Id
            );

            account.UpdateBalance(normalizedAmount);

            await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
            try
            {
                await _investimentoRepository.AddAsync(investimento);
                await _transactionRepository.AddAsync(originTransaction);
                _accountRepository.Update(account);

                await _transactionRepository.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

            return MapToDto(investimento);
        }

        public async Task<IEnumerable<InvestimentoResponseDto>> GetUserInvestimentosAsync(Guid userId)
        {
            var investimentos = (await _investimentoRepository.GetAllByUserIdAsync(userId)).ToList();

            var desde = DateTime.UtcNow.Date.AddMonths(-3);
            var cotacoesPorInvestimento = (await _cotacaoHistoricoRepository.GetByInvestimentoIdsSinceAsync(
                    investimentos.Select(i => i.Id), desde))
                .GroupBy(c => c.InvestimentoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Data).ToList());

            return investimentos.Select(investimento =>
                MapToDto(investimento, cotacoesPorInvestimento.GetValueOrDefault(investimento.Id, new List<CotacaoHistorico>())));
        }

        public async Task<InvestimentoResponseDto> UpdateValorAtualAsync(Guid investimentoId, Guid userId, decimal novoValor)
        {
            var investimento = await _investimentoRepository.GetByIdAsync(investimentoId);
            if (investimento == null || investimento.UserId != userId)
                throw new UnauthorizedAccessException("Investimento não encontrado ou não pertence ao usuário.");

            // Reprecificação de mercado: altera apenas o valor atual, sem movimentar caixa.
            investimento.AtualizarValorAtual(novoValor);
            _investimentoRepository.Update(investimento);
            await _investimentoRepository.SaveChangesAsync();
            return MapToDto(investimento);
        }

        public async Task DeleteInvestimentoAsync(Guid investimentoId, Guid userId)
        {
            var investimento = await _investimentoRepository.GetByIdAsync(investimentoId);
            if (investimento == null || investimento.UserId != userId)
                throw new UnauthorizedAccessException("Investimento não encontrado ou não pertence ao usuário.");

            // Excluir o investimento estorna o dinheiro à(s) conta(s) de origem,
            // removendo as transações de aporte vinculadas (mesma lógica das metas).
            var aportes = (await _transactionRepository.GetByInvestimentoIdAsync(investimentoId)).ToList();

            await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
            try
            {
                foreach (var group in aportes.GroupBy(t => t.AccountId))
                {
                    var account = await _accountRepository.GetByIdAsync(group.Key, userId);
                    if (account != null)
                    {
                        // Amount é negativo (débito); negá-lo restaura o saldo.
                        var totalToRestore = group.Sum(t => -t.Amount);
                        account.UpdateBalance(totalToRestore);
                        _accountRepository.Update(account);
                    }
                }

                foreach (var aporte in aportes)
                    _transactionRepository.Delete(aporte);

                _investimentoRepository.Delete(investimento);

                await _transactionRepository.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InvestimentoResponseDto> AdicionarAporteAsync(Guid investimentoId, Guid userId, AporteInvestimentoRequestDto request)
        {
            var investimento = await _investimentoRepository.GetByIdAsync(investimentoId);
            if (investimento == null || investimento.UserId != userId)
                throw new UnauthorizedAccessException("Investimento não encontrado ou não pertence ao usuário.");

            var account = await _accountRepository.GetByIdAsync(request.AccountId, userId);
            if (account == null)
                throw new InvalidOperationException("Conta de origem não encontrada ou não pertence ao usuário.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, userId);
            if (category == null)
                throw new InvalidOperationException("Categoria não encontrada ou não pertence ao usuário.");

            if (account.Balance < request.Valor)
                throw new InvalidOperationException(
                    $"Saldo insuficiente na conta \"{account.Name}\". Disponível: {account.Balance:C}.");

            var normalizedAmount = -Math.Abs(request.Valor); // Investment debita da conta
            var aporteTransaction = new Transaction(
                description: $"Aporte em investimento: {investimento.Nome}",
                amount: normalizedAmount,
                type: TransactionType.Investment,
                date: request.Data ?? DateTime.UtcNow,
                accountId: request.AccountId,
                categoryId: request.CategoryId,
                financialGoalId: null,
                investimentoId: investimento.Id
            );

            investimento.AdicionarAporte(request.Valor);
            account.UpdateBalance(normalizedAmount);

            await using var dbTransaction = await _transactionRepository.BeginTransactionAsync();
            try
            {
                await _transactionRepository.AddAsync(aporteTransaction);
                _accountRepository.Update(account);
                _investimentoRepository.Update(investimento);

                await _transactionRepository.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

            return MapToDto(investimento);
        }

        public async Task<IEnumerable<AporteHistoricoResponseDto>> GetHistoricoAportesAsync(Guid investimentoId, Guid userId)
        {
            var investimento = await _investimentoRepository.GetByIdAsync(investimentoId);
            if (investimento == null || investimento.UserId != userId)
                throw new UnauthorizedAccessException("Investimento não encontrado ou não pertence ao usuário.");

            var aportes = await _transactionRepository.GetByInvestimentoIdAsync(investimentoId);

            return aportes
                .OrderByDescending(t => t.Date)
                .Select(t => new AporteHistoricoResponseDto
                {
                    TransactionId = t.Id,
                    Valor = Math.Abs(t.Amount),
                    Data = t.Date,
                    ContaNome = t.Account?.Name
                });
        }

        private static InvestimentoResponseDto MapToDto(Investimento investimento, IReadOnlyList<CotacaoHistorico>? cotacoes = null)
        {
            cotacoes ??= Array.Empty<CotacaoHistorico>();

            decimal? variacao3Meses = null;
            if (cotacoes.Count > 0 && cotacoes[0].Valor > 0)
            {
                var basePreco = cotacoes[0].Valor;
                var precoAtual = cotacoes[^1].Valor;
                variacao3Meses = (precoAtual - basePreco) / basePreco * 100;
            }

            return new InvestimentoResponseDto
            {
                Id = investimento.Id,
                Nome = investimento.Nome,
                TotalAportado = investimento.TotalAportado,
                ValorAtual = investimento.ValorAtual,
                Tipo = investimento.Tipo,
                DataCriacao = investimento.DataCriacao,
                RentabilidadePercentual = investimento.RentabilidadePercentual,
                Ticker = investimento.Ticker,
                VariacaoUltimos3MesesPercentual = variacao3Meses,
                HistoricoCotacoes = cotacoes.Select(c => new CotacaoPontoDto { Data = c.Data, Valor = c.Valor }).ToList()
            };
        }
    }
}
