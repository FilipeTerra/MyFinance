using System;
using System.Collections.Generic;
using System.Linq;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Tabela de referência das faixas do Minha Casa Minha Vida. É `static
    /// readonly` em vez de vir do banco de propósito: os valores mudam por
    /// decreto do governo, não por ação de usuário, e persistir isso como se
    /// fosse dado de negócio seria complexidade sem benefício — atualizar exige
    /// editar este arquivo e mudar <see cref="VigenciaReferencia"/>, o mesmo
    /// esforço de uma migration, sem precisar de uma.
    /// </summary>
    public static class MinhaCasaMinhaVidaTabela
    {
        /// <summary>Mês de referência dos valores desta tabela — exibido na UI para o usuário saber a idade do dado.</summary>
        public const string VigenciaReferencia = "2026-01";

        public static readonly IReadOnlyList<FaixaMcmv> Faixas = new[]
        {
            new FaixaMcmv(FaixaMinhaCasaMinhaVida.Faixa1, 3200m, 4.00m, 5.25m, 264000m, 10m, SubsidioMaximoPercentualImovel: 95m),
            new FaixaMcmv(FaixaMinhaCasaMinhaVida.Faixa2, 5000m, 4.75m, 7.00m, 264000m, 10m, SubsidioMaximoValor: 55000m),
            new FaixaMcmv(FaixaMinhaCasaMinhaVida.Faixa3, 9600m, 7.66m, 8.16m, 400000m, 20m),
            new FaixaMcmv(FaixaMinhaCasaMinhaVida.Faixa4, 13000m, 10.00m, 10.50m, 600000m, 20m)
        };

        /// <summary>Maior renda ainda coberta por alguma faixa. Acima disso o programa não se aplica.</summary>
        public static decimal RendaMaximaDoPrograma => Faixas[^1].RendaMaxima;

        /// <summary>
        /// Resolve a faixa pela renda familiar bruta mensal. Nulo quando a renda
        /// está fora do programa (não positiva, ou acima do teto da Faixa 4) —
        /// nunca lança, é o chamador que decide o que fazer com "sem faixa".
        /// </summary>
        public static FaixaMcmv? ResolverFaixa(decimal rendaMensal)
        {
            if (rendaMensal <= 0)
                return null;

            return Faixas.FirstOrDefault(f => rendaMensal <= f.RendaMaxima);
        }

        /// <summary>
        /// Teto da faixa de taxa, convertido de anual para mensal equivalente —
        /// a taxa usada quando o usuário não informa a sua. É o teto, não uma
        /// média: nunca promete uma parcela menor do que a pior taxa da faixa.
        /// </summary>
        public static decimal TaxaTetoMensalPercentual(FaixaMcmv faixa)
        {
            var anual = faixa.TaxaAnualMaximaPercentual / 100;
            var mensal = (decimal)Math.Pow((double)(1 + anual), 1.0 / 12) - 1;
            return Math.Round(mensal * 100, 4);
        }
    }
}
