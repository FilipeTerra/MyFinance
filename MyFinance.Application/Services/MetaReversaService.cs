using System;
using System.Threading.Tasks;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Repositories;
using MyFinance.Application.Interfaces.Services;

namespace MyFinance.Application.Services
{
    /// <summary>
    /// "Meta reversa": em vez de projetar um resultado a partir de aporte+prazo,
    /// resolve o aporte mensal ou o prazo necessários para atingir um valor-alvo,
    /// via busca binária sobre <see cref="IProjecaoInvestimentoService"/> — reaproveita
    /// toda a lógica tributária existente (IR, IOF, come-cotas, ganho de capital,
    /// previdência) sem duplicá-la, tratando a projeção como uma caixa-preta
    /// monotonicamente crescente em aporte e em prazo.
    /// </summary>
    /// <remarks>
    /// Categorias de ganho de capital (Ação/Cripto) têm um "degrau" de isenção por
    /// faixa de venda: o valor líquido pode cair abruptamente assim que o valor
    /// final cruza o limite de isenção (R$20mil/R$35mil), já que a isenção é
    /// tudo-ou-nada, não marginal. A busca binária assume monotonicidade e pode
    /// convergir para um resultado ligeiramente subótimo bem em cima desse degrau —
    /// uma limitação aceita, não um bug, e irrelevante para as demais categorias.
    /// </remarks>
    public class MetaReversaService : IMetaReversaService
    {
        private readonly IProjecaoInvestimentoService _projecaoService;
        private readonly IFinancialGoalRepository _goalRepository;

        private const int MaxPrazoMesesBusca = 600; // 50 anos
        private const int MaxIteracoes = 60;

        public MetaReversaService(IProjecaoInvestimentoService projecaoService, IFinancialGoalRepository goalRepository)
        {
            _projecaoService = projecaoService;
            _goalRepository = goalRepository;
        }

        public async Task<AporteNecessarioResponseDto> CalcularAporteNecessarioAsync(CalcularAporteNecessarioRequestDto request)
        {
            if (request.ValorAlvo <= 0)
                throw new ArgumentException("O valor-alvo deve ser maior que zero.", nameof(request.ValorAlvo));

            Task<ProjecaoInvestimentoResponseDto> SimularAsync(decimal aporteMensal) => _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
            {
                AporteInicial = request.AporteInicial,
                AporteMensal = aporteMensal,
                PrazoMeses = request.PrazoMeses,
                FonteTaxaJuros = request.FonteTaxaJuros,
                TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
                PercentualCdi = request.PercentualCdi,
                TipoAtivo = request.TipoAtivo
            });

            var semAporte = await SimularAsync(0m);
            if (semAporte.ValorFinalLiquido >= request.ValorAlvo)
                return new AporteNecessarioResponseDto { AporteMensalNecessario = 0m, Projecao = semAporte };

            // Expande o limite superior até ultrapassar o alvo (busca exponencial).
            var hi = Math.Max(request.ValorAlvo / Math.Max(request.PrazoMeses, 1), 10m);
            var resultadoHi = await SimularAsync(hi);
            for (var i = 0; resultadoHi.ValorFinalLiquido < request.ValorAlvo && i < MaxIteracoes; i++)
            {
                hi *= 2;
                resultadoHi = await SimularAsync(hi);
            }

            // Precisão do aporte proporcional ao valor-alvo (0,01%, piso R$ 0,01) — um valor
            // fixo (ex.: R$ 0,50) distorceria metas pequenas, já que cada real de aporte a
            // mais se multiplica pelo fator de juros compostos ao longo do prazo.
            var precisaoAporte = Math.Max(0.01m, request.ValorAlvo * 0.0001m);

            var lo = 0m;
            var melhorResultado = resultadoHi;
            for (var i = 0; i < MaxIteracoes && hi - lo > precisaoAporte; i++)
            {
                var mid = lo + (hi - lo) / 2;
                var resultadoMid = await SimularAsync(mid);
                if (resultadoMid.ValorFinalLiquido < request.ValorAlvo)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                    melhorResultado = resultadoMid;
                }
            }

            return new AporteNecessarioResponseDto { AporteMensalNecessario = Math.Round(hi, 2), Projecao = melhorResultado };
        }

        public async Task<PrazoNecessarioResponseDto> CalcularPrazoNecessarioAsync(CalcularPrazoNecessarioRequestDto request)
        {
            if (request.ValorAlvo <= 0)
                throw new ArgumentException("O valor-alvo deve ser maior que zero.", nameof(request.ValorAlvo));

            Task<ProjecaoInvestimentoResponseDto> SimularAsync(int prazoMeses) => _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
            {
                AporteInicial = request.AporteInicial,
                AporteMensal = request.AporteMensal,
                PrazoMeses = prazoMeses,
                FonteTaxaJuros = request.FonteTaxaJuros,
                TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
                PercentualCdi = request.PercentualCdi,
                TipoAtivo = request.TipoAtivo
            });

            var resultadoMax = await SimularAsync(MaxPrazoMesesBusca);
            if (resultadoMax.ValorFinalLiquido < request.ValorAlvo)
                return new PrazoNecessarioResponseDto { Atingivel = false };

            var lo = 1;
            var hi = MaxPrazoMesesBusca;
            var melhorResultado = resultadoMax;
            while (lo < hi)
            {
                var mid = lo + (hi - lo) / 2;
                var resultadoMid = await SimularAsync(mid);
                if (resultadoMid.ValorFinalLiquido < request.ValorAlvo)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                    melhorResultado = resultadoMid;
                }
            }

            return new PrazoNecessarioResponseDto { Atingivel = true, PrazoMesesNecessario = hi, Projecao = melhorResultado };
        }

        public async Task<SimularMetaResponseDto> SimularMetaAsync(Guid goalId, Guid userId, SimularMetaRequestDto request)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId);
            if (goal == null || goal.UserId != userId)
                throw new UnauthorizedAccessException("Meta não encontrada ou não pertence ao usuário.");

            var hoje = DateTime.UtcNow;
            var prazoMeses = ((goal.Deadline.Year - hoje.Year) * 12) + (goal.Deadline.Month - hoje.Month);
            if (prazoMeses <= 0)
                throw new InvalidOperationException("O prazo da meta já venceu ou vence neste mês — não é possível simular.");

            if (request.AporteMensal.HasValue)
            {
                var projecao = await _projecaoService.CalcularProjecaoAsync(new CalcularProjecaoRequestDto
                {
                    AporteInicial = goal.CurrentAmount,
                    AporteMensal = request.AporteMensal.Value,
                    PrazoMeses = prazoMeses,
                    FonteTaxaJuros = request.FonteTaxaJuros,
                    TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
                    PercentualCdi = request.PercentualCdi,
                    TipoAtivo = request.TipoAtivo
                });

                return new SimularMetaResponseDto
                {
                    PrazoMesesRestante = prazoMeses,
                    Atinge = projecao.ValorFinalLiquido >= goal.TargetAmount,
                    DiferencaParaMeta = projecao.ValorFinalLiquido - goal.TargetAmount,
                    Projecao = projecao
                };
            }

            var aporteNecessario = await CalcularAporteNecessarioAsync(new CalcularAporteNecessarioRequestDto
            {
                AporteInicial = goal.CurrentAmount,
                ValorAlvo = goal.TargetAmount,
                PrazoMeses = prazoMeses,
                FonteTaxaJuros = request.FonteTaxaJuros,
                TaxaJurosAnualPercentual = request.TaxaJurosAnualPercentual,
                PercentualCdi = request.PercentualCdi,
                TipoAtivo = request.TipoAtivo
            });

            return new SimularMetaResponseDto
            {
                PrazoMesesRestante = prazoMeses,
                Atinge = true,
                AporteMensalNecessario = aporteNecessario.AporteMensalNecessario,
                DiferencaParaMeta = 0m,
                Projecao = aporteNecessario.Projecao
            };
        }
    }
}
