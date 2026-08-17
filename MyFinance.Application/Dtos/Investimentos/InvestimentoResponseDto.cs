using System;
using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Investimentos
{
    public class InvestimentoResponseDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        /// <summary>
        /// Soma de todo o dinheiro já aportado neste investimento (aporte inicial + aportes adicionais).
        /// </summary>
        public decimal TotalAportado { get; set; }
        public decimal ValorAtual { get; set; }
        public InvestmentType Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
        public decimal RentabilidadePercentual { get; set; }
        public string? Ticker { get; set; }

        /// <summary>
        /// Variação percentual da cotação nos últimos 3 meses. Nulo quando não há
        /// histórico de cotações suficiente (ex: Renda Fixa ou investimento recém-criado).
        /// </summary>
        public decimal? VariacaoUltimos3MesesPercentual { get; set; }

        /// <summary>
        /// Série de cotações dos últimos 3 meses, usada para o mini-gráfico do card.
        /// </summary>
        public IEnumerable<CotacaoPontoDto> HistoricoCotacoes { get; set; } = Array.Empty<CotacaoPontoDto>();
    }
}
