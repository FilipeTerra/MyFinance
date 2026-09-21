using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Financiamento
{
    public class ResultadoFinanciamentoDto
    {
        public decimal PrimeiraParcela { get; set; }
        public decimal UltimaParcela { get; set; }
        public decimal TotalPago { get; set; }
        public decimal TotalJuros { get; set; }

        /// <summary>
        /// Juros totais como percentual do valor financiado. <b>Não é o CET</b>:
        /// não considera seguros, tarifas nem o efeito do tempo sobre o dinheiro.
        /// </summary>
        public decimal JurosSobreFinanciadoPercentual { get; set; }

        public List<ParcelaFinanciamentoDto> Parcelas { get; set; } = new();

        /// <summary>Em quantos meses o contrato quitou de fato.</summary>
        public int PrazoFinalMeses { get; set; }

        /// <summary>Soma de tudo que foi pago além das parcelas contratuais.</summary>
        public decimal TotalAmortizacaoExtra { get; set; }

        /// <summary>Juros economizados em relação ao mesmo contrato sem amortização extra.</summary>
        public decimal EconomiaJuros { get; set; }

        /// <summary>Quantos meses a quitação foi antecipada pela amortização extra.</summary>
        public int MesesEconomizados { get; set; }

        /// <summary>Total pago em seguros obrigatórios (MIP + DFI) ao longo do contrato.</summary>
        public decimal TotalSeguros { get; set; }

        /// <summary>Total pago em tarifa de administração ao longo do contrato.</summary>
        public decimal TotalTaxaAdministracao { get; set; }

        /// <summary>
        /// Desembolso do primeiro mês já com extras, seguros e tarifa — é o valor
        /// que aparece no boleto, e o que deve ser comparado com a renda.
        /// </summary>
        public decimal PrimeiraParcelaTotal { get; set; }

        /// <summary>Tudo que sai do bolso no contrato: parcelas, extras, seguros e tarifas.</summary>
        public decimal TotalDesembolsado { get; set; }

        /// <summary>
        /// Custo Efetivo Total anual: a taxa real do contrato, considerando
        /// seguros, tarifas e o efeito do tempo sobre o dinheiro — o número certo
        /// para comparar propostas de bancos diferentes.
        /// </summary>
        public decimal CetAnualPercentual { get; set; }

        public decimal CetMensalPercentual { get; set; }

        /// <summary>Falso quando a TIR não convergiu. O front deve exibir "—", nunca 0%.</summary>
        public bool CetConvergiu { get; set; }
    }
}
