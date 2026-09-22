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
    /// <param name="Itbi">Imposto sobre a transmissão do imóvel, pago ao município — fora do CET, é custo do negócio.</param>
    /// <param name="CustosCartorio">Escritura e registro somados.</param>
    /// <param name="Subsidio">
    /// Subsídio do MCMV informado pelo usuário (o que a Caixa ofereceu), em R$.
    /// Reduz o financiado como uma entrada paga pelo governo — mas não é a
    /// entrada do comprador, então não entra em <see cref="EntradaPercentual"/>
    /// nem em <see cref="DesembolsoInicial"/>: quem paga é o programa, não o bolso do comprador.
    /// </param>
    public record ComposicaoFinanciamento(
        decimal ValorImovel,
        decimal Entrada,
        decimal EntradaPercentual,
        decimal ValorFinanciado,
        decimal Itbi = 0m,
        decimal CustosCartorio = 0m,
        decimal Subsidio = 0m)
    {
        /// <summary>
        /// Tudo que precisa estar em caixa além do financiamento em si: a entrada
        /// mais os custos de fechar o negócio. É o número que responde "quanto eu
        /// preciso ter guardado", não só "quanto eu financio".
        /// </summary>
        public decimal DesembolsoInicial => Entrada + Itbi + CustosCartorio;

        /// <summary>
        /// Resolve a composição a partir do valor do imóvel e da entrada, que o
        /// usuário pode informar em reais ou em percentual.
        /// </summary>
        /// <param name="valorImovel">Preço do imóvel, em R$.</param>
        /// <param name="entradaValor">Entrada em R$. Tem precedência sobre <paramref name="entradaPercentual"/>.</param>
        /// <param name="entradaPercentual">Entrada como % do imóvel. Usada só quando <paramref name="entradaValor"/> é nulo.</param>
        /// <param name="itbi">Imposto de transmissão, em R$. Não afeta o financiado — é custo de fechamento.</param>
        /// <param name="custosCartorio">Escritura + registro, em R$.</param>
        /// <param name="subsidio">Subsídio do MCMV, em R$. Abate o financiado igual à entrada.</param>
        public static ComposicaoFinanciamento Resolver(
            decimal valorImovel,
            decimal? entradaValor,
            decimal? entradaPercentual,
            decimal itbi = 0m,
            decimal custosCartorio = 0m,
            decimal subsidio = 0m)
        {
            if (valorImovel <= 0)
                throw new ArgumentException("O valor do imóvel deve ser maior que zero.", nameof(valorImovel));

            var entrada = entradaValor ?? PercentualParaValor(valorImovel, entradaPercentual);

            if (entrada < 0)
                throw new ArgumentException("A entrada não pode ser negativa.", nameof(entradaValor));

            if (itbi < 0)
                throw new ArgumentException("O ITBI não pode ser negativo.", nameof(itbi));

            if (custosCartorio < 0)
                throw new ArgumentException("Os custos de cartório não podem ser negativos.", nameof(custosCartorio));

            if (subsidio < 0)
                throw new ArgumentException("O subsídio não pode ser negativo.", nameof(subsidio));

            entrada = Math.Round(entrada, 2);
            subsidio = Math.Round(subsidio, 2);

            if (entrada + subsidio >= valorImovel)
                throw new ArgumentException(
                    "A entrada somada ao subsídio não pode ser maior ou igual ao valor do imóvel — não sobraria nada a financiar.",
                    nameof(entradaValor));

            var financiado = Math.Round(valorImovel - entrada - subsidio, 2);
            var percentual = Math.Round(entrada / valorImovel * 100, 2);

            return new ComposicaoFinanciamento(valorImovel, entrada, percentual, financiado, itbi, custosCartorio, subsidio);
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
