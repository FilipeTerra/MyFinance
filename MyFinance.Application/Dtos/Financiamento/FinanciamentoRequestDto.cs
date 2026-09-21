using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Financiamento
{
    /// <summary>Parâmetros de entrada para simular um financiamento pelos sistemas Price e SAC.</summary>
    public record FinanciamentoRequestDto
    {
        /// <summary>
        /// Principal financiado, em R$. Ignorado quando <see cref="ValorImovel"/>
        /// é informado — nesse caso o financiado é derivado (imóvel menos entrada).
        /// </summary>
        public decimal ValorFinanciado { get; init; }

        /// <summary>Taxa de juros do contrato, ao mês, em % (ex.: 1.5 para 1,5% a.m.).</summary>
        public decimal TaxaJurosMensalPercentual { get; init; }

        /// <summary>Número de parcelas mensais contratadas.</summary>
        public int NumParcelas { get; init; }

        /// <summary>
        /// Preço do imóvel, em R$. Quando informado, passa a ser a base do cálculo
        /// e o valor financiado vira o que sobra depois da entrada.
        /// </summary>
        public decimal? ValorImovel { get; init; }

        /// <summary>Entrada em R$. Tem precedência sobre <see cref="EntradaPercentual"/>.</summary>
        public decimal? Entrada { get; init; }

        /// <summary>Entrada como % do imóvel. Usada só quando <see cref="Entrada"/> é nula.</summary>
        public decimal? EntradaPercentual { get; init; }

        /// <summary>Valor pago a mais em toda parcela, abatido direto do principal.</summary>
        public decimal AmortizacaoExtraMensal { get; init; }

        /// <summary>Pagamentos extras pontuais (FGTS, 13º), por mês.</summary>
        public List<AmortizacaoExtraAvulsaDto>? AmortizacoesExtrasAvulsas { get; init; }

        /// <summary>Se o pagamento extra encurta o prazo ou reduz a parcela. Padrão: encurtar o prazo.</summary>
        public ModoAmortizacaoExtra ModoAmortizacaoExtra { get; init; } = ModoAmortizacaoExtra.ReduzirPrazo;

        /// <summary>Alíquota mensal do seguro MIP sobre o saldo devedor, em % (ex.: 0.025).</summary>
        public decimal SeguroMipMensalPercentualSaldo { get; init; }

        /// <summary>Alíquota mensal do seguro DFI sobre o valor do imóvel, em % (ex.: 0.01).</summary>
        public decimal SeguroDfiMensalPercentualImovel { get; init; }

        /// <summary>Tarifa mensal de administração do contrato, em R$.</summary>
        public decimal TaxaAdministracaoMensal { get; init; }

        /// <summary>
        /// Tarifas cobradas pelo credor na contratação (avaliação do imóvel, IOF),
        /// descontadas do valor liberado. Não inclui ITBI, escritura ou registro —
        /// esses vão para o Estado e o cartório, não para o banco.
        /// </summary>
        public decimal TarifasContratacao { get; init; }
    }
}
