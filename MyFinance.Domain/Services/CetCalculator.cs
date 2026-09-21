using System.Collections.Generic;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Custo Efetivo Total: a taxa que zera o valor presente do fluxo de caixa do
    /// contrato — o que o tomador recebe contra tudo que ele paga, incluindo
    /// seguros e tarifas. É o número certo para comparar duas propostas de banco
    /// diferentes; a taxa de contrato sozinha não é, porque não enxerga encargos.
    /// </summary>
    /// <remarks>
    /// Resolvido por bisseção, não por Newton-Raphson: para um fluxo convencional
    /// (um recebimento em t=0, só pagamentos depois) o valor presente líquido é
    /// estritamente crescente na taxa — quanto maior a taxa, menos pesam os
    /// pagamentos futuros — então há exatamente uma raiz e a bisseção converge
    /// sempre, sem precisar de derivada nem arriscar divergir com um chute inicial
    /// ruim. Mesmo raciocínio da busca binária de <c>MetaReversaService</c>.
    ///
    /// O cálculo interno usa <see cref="double"/>, não <see cref="decimal"/>: a
    /// busca do teto pode avaliar taxas absurdamente altas antes de convergir
    /// (chega a dobrar até 1000% a.m.), e elevar um fator a centenas de meses
    /// nessa faixa estoura o range de <see cref="decimal"/>. Em double o mesmo
    /// cálculo satura para infinito sem lançar exceção — exatamente o
    /// comportamento correto (pagamento futuro descontado a uma taxa absurda vale
    /// zero). Só o resultado final volta a <see cref="decimal"/>, arredondado.
    /// </remarks>
    public static class CetCalculator
    {
        private const int MaxIteracoes = 60;

        /// <summary>Teto de sanidade da busca: 1000% ao mês. Além disso, o resultado não seria um número útil.</summary>
        private const double TaxaMensalMaxima = 10.0;

        private const double Tolerancia = 1e-10;

        /// <param name="Convergiu">
        /// Falso quando a TIR não pôde ser calculada com confiança — nunca um
        /// valor inventado. O chamador deve exibir "—", não 0%.
        /// </param>
        public record ResultadoCet(bool Convergiu, decimal TaxaMensalPercentual, decimal TaxaAnualPercentual);

        private static readonly ResultadoCet NaoConvergiu = new(false, 0m, 0m);

        /// <param name="fluxoCaixa">
        /// Índice = mês. Posição 0 positiva (valor líquido liberado ao tomador);
        /// posições seguintes negativas (desembolsos mensais, já com extras,
        /// seguros e tarifas incluídos).
        /// </param>
        public static ResultadoCet Calcular(IReadOnlyList<decimal> fluxoCaixa)
        {
            if (fluxoCaixa.Count < 2 || fluxoCaixa[0] <= 0)
                return NaoConvergiu;

            var vplNaTaxaZero = ValorPresenteLiquido(fluxoCaixa, 0);

            // VPL(0%) é o líquido do contrato sem desconto algum: liberado menos
            // tudo que se paga. Maior que zero significa que o tomador recebeu
            // mais do que devolveu — não existe taxa positiva que explique isso,
            // e taxa negativa está fora do que "custo efetivo" significa aqui.
            if (vplNaTaxaZero > 0)
                return NaoConvergiu;

            // Exatamente zero é o caso de um contrato sem custo algum (taxa 0%,
            // sem seguro, sem tarifa) — a raiz é 0% mesmo, não "não converge".
            if (vplNaTaxaZero == 0)
                return Convergido(0);

            var lo = 0.0;
            var hi = 0.01;
            while (ValorPresenteLiquido(fluxoCaixa, hi) < 0)
            {
                hi *= 2;
                if (hi > TaxaMensalMaxima)
                    return NaoConvergiu;
            }

            for (var iteracao = 0; iteracao < MaxIteracoes && hi - lo > Tolerancia; iteracao++)
            {
                var meio = (lo + hi) / 2;
                if (ValorPresenteLiquido(fluxoCaixa, meio) < 0)
                    lo = meio;
                else
                    hi = meio;
            }

            return Convergido((lo + hi) / 2);
        }

        private static ResultadoCet Convergido(double taxaMensal)
        {
            var mensal = (decimal)(taxaMensal * 100);
            var anual = (decimal)((System.Math.Pow(1 + taxaMensal, 12) - 1) * 100);
            return new ResultadoCet(true, System.Math.Round(mensal, 4), System.Math.Round(anual, 2));
        }

        private static double ValorPresenteLiquido(IReadOnlyList<decimal> fluxoCaixa, double taxaMensal)
        {
            var vpl = 0.0;
            var fator = 1.0;
            var divisor = 1 + taxaMensal;

            for (var t = 0; t < fluxoCaixa.Count; t++)
            {
                vpl += (double)fluxoCaixa[t] / fator;
                fator *= divisor;
            }

            return vpl;
        }
    }
}
