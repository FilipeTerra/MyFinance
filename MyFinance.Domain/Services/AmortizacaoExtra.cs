using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Pagamento extra avulso abatido do saldo devedor em um mês específico —
    /// o uso típico é o FGTS, liberado para amortizar a cada dois anos, ou o 13º.
    /// </summary>
    public record AmortizacaoExtraAvulsa(int Mes, decimal Valor);

    /// <summary>
    /// Quanto o mutuário paga além da parcela contratual e o que esse abatimento
    /// faz com o contrato. O valor mensal é somado a toda parcela; as avulsas
    /// entram só nos meses indicados. Ambos abatem o principal diretamente, sem
    /// passar por juros — é isso que gera a economia.
    /// </summary>
    /// <param name="ValorMensal">Valor extra pago todo mês, junto da parcela. Zero quando não há.</param>
    /// <param name="Avulsas">Pagamentos extras pontuais. Nulo ou vazio quando não há.</param>
    /// <param name="Modo">Se o abatimento encurta o prazo ou reduz a parcela.</param>
    public record AmortizacaoExtra(
        decimal ValorMensal,
        IReadOnlyList<AmortizacaoExtraAvulsa>? Avulsas,
        ModoAmortizacaoExtra Modo)
    {
        /// <summary>Indica se há de fato algum valor extra a aplicar.</summary>
        public bool TemAlgumValor => ValorMensal > 0 || (Avulsas is not null && Avulsas.Count > 0);
    }
}
