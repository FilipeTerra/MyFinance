using System;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Como o valor do imóvel se decompõe: o que sai do bolso do comprador
    /// (entrada) e o que sobra para o banco financiar. Só a parcela financiada
    /// entra no cronograma de amortização — a entrada não paga juros, e é
    /// justamente por isso que ela muda tanto o custo total do negócio.
    /// </summary>
    /// <param name="EntradaPercentual">Entrada como percentual do imóvel, já resolvida (0 a 100).</param>
    public record ComposicaoFinanciamento(
        decimal ValorImovel,
        decimal Entrada,
        decimal EntradaPercentual,
        decimal ValorFinanciado)
    {
        /// <summary>
        /// Resolve a composição a partir do valor do imóvel e da entrada, que o
        /// usuário pode informar em reais ou em percentual.
        /// </summary>
        /// <param name="valorImovel">Preço do imóvel, em R$.</param>
        /// <param name="entradaValor">Entrada em R$. Tem precedência sobre <paramref name="entradaPercentual"/>.</param>
        /// <param name="entradaPercentual">Entrada como % do imóvel. Usada só quando <paramref name="entradaValor"/> é nulo.</param>
        public static ComposicaoFinanciamento Resolver(
            decimal valorImovel,
            decimal? entradaValor,
            decimal? entradaPercentual)
        {
            if (valorImovel <= 0)
                throw new ArgumentException("O valor do imóvel deve ser maior que zero.", nameof(valorImovel));

            var entrada = entradaValor ?? PercentualParaValor(valorImovel, entradaPercentual);

            if (entrada < 0)
                throw new ArgumentException("A entrada não pode ser negativa.", nameof(entradaValor));

            if (entrada >= valorImovel)
                throw new ArgumentException(
                    "A entrada não pode ser maior ou igual ao valor do imóvel — não sobraria nada a financiar.",
                    nameof(entradaValor));

            entrada = Math.Round(entrada, 2);
            var financiado = Math.Round(valorImovel - entrada, 2);
            var percentual = Math.Round(entrada / valorImovel * 100, 2);

            return new ComposicaoFinanciamento(valorImovel, entrada, percentual, financiado);
        }

        private static decimal PercentualParaValor(decimal valorImovel, decimal? percentual)
        {
            if (percentual is null)
                return 0m;

            if (percentual < 0 || percentual >= 100)
                throw new ArgumentException(
                    "O percentual de entrada deve estar entre 0 e 100.", nameof(percentual));

            return valorImovel * percentual.Value / 100;
        }
    }
}
