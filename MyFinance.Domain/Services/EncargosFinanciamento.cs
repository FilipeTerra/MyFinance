using System;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Encargos cobrados por cima da parcela contratual num financiamento
    /// imobiliário e que <b>não abatem o saldo devedor</b> — é por isso que a
    /// parcela que o banco cobra é maior que a que sai da fórmula do Price ou
    /// do SAC, e por isso que compará-los sem eles subestima o custo real.
    ///
    /// O MIP (morte e invalidez permanente) incide sobre o saldo devedor, então
    /// cai junto com a dívida; o DFI (danos físicos ao imóvel) incide sobre o
    /// valor do imóvel, ficando praticamente constante; a taxa de administração
    /// é um valor fixo em reais.
    /// </summary>
    /// <param name="SeguroMipMensalPercentualSaldo">Alíquota mensal do MIP sobre o saldo devedor, em % (ex.: 0.025).</param>
    /// <param name="SeguroDfiMensalPercentualImovel">Alíquota mensal do DFI sobre o valor do imóvel, em % (ex.: 0.01).</param>
    /// <param name="ValorImovel">Base de cálculo do DFI. Zero quando o usuário não informou o preço do imóvel.</param>
    /// <param name="TaxaAdministracaoMensal">Tarifa mensal fixa, em R$.</param>
    public record EncargosFinanciamento(
        decimal SeguroMipMensalPercentualSaldo,
        decimal SeguroDfiMensalPercentualImovel,
        decimal ValorImovel,
        decimal TaxaAdministracaoMensal)
    {
        /// <summary>Indica se há de fato algum encargo a cobrar.</summary>
        public bool TemAlgumValor =>
            SeguroMipMensalPercentualSaldo > 0
            || (SeguroDfiMensalPercentualImovel > 0 && ValorImovel > 0)
            || TaxaAdministracaoMensal > 0;

        /// <summary>Valida as alíquotas. Percentual negativo é erro de entrada, não algo a normalizar.</summary>
        public void Validar()
        {
            if (SeguroMipMensalPercentualSaldo < 0)
                throw new ArgumentException(
                    "A alíquota do seguro MIP não pode ser negativa.", nameof(SeguroMipMensalPercentualSaldo));

            if (SeguroDfiMensalPercentualImovel < 0)
                throw new ArgumentException(
                    "A alíquota do seguro DFI não pode ser negativa.", nameof(SeguroDfiMensalPercentualImovel));

            if (TaxaAdministracaoMensal < 0)
                throw new ArgumentException(
                    "A taxa de administração não pode ser negativa.", nameof(TaxaAdministracaoMensal));
        }

        /// <summary>Seguro MIP do mês, sobre o saldo devedor de abertura.</summary>
        public decimal MipDoMes(decimal saldoDevedorAbertura) =>
            Math.Round(saldoDevedorAbertura * SeguroMipMensalPercentualSaldo / 100, 2);

        /// <summary>Seguro DFI do mês, sobre o valor do imóvel.</summary>
        public decimal DfiDoMes() =>
            Math.Round(ValorImovel * SeguroDfiMensalPercentualImovel / 100, 2);
    }
}
