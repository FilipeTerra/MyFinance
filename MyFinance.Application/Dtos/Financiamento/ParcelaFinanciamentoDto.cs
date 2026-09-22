namespace MyFinance.Application.Dtos.Financiamento
{
    public class ParcelaFinanciamentoDto
    {
        public int Numero { get; set; }

        /// <summary>Parcela contratual do mês (juros + amortização), sem o pagamento extra.</summary>
        public decimal ValorParcela { get; set; }

        public decimal Juros { get; set; }
        public decimal Amortizacao { get; set; }
        public decimal SaldoDevedor { get; set; }

        /// <summary>Pagamento extra do mês, abatido direto do principal.</summary>
        public decimal AmortizacaoExtra { get; set; }

        /// <summary>Seguro de morte e invalidez do mês, sobre o saldo devedor.</summary>
        public decimal SeguroMip { get; set; }

        /// <summary>Seguro de danos físicos ao imóvel do mês.</summary>
        public decimal SeguroDfi { get; set; }

        /// <summary>Tarifa mensal de administração do contrato.</summary>
        public decimal TaxaAdministracao { get; set; }

        /// <summary>Desembolso real do mês: parcela contratual + extra + seguros + tarifa.</summary>
        public decimal ParcelaTotal { get; set; }
    }
}
