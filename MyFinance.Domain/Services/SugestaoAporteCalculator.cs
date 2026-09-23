using System;
using System.Collections.Generic;
using System.Linq;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Confronta o aporte mensal exigido por uma meta com o orçamento real do usuário
    /// e responde se ele cabe; quando não cabe, monta um plano de corte sobre as
    /// categorias que o próprio usuário marcou como discricionárias.
    /// Cálculo puro: recebe as médias mensais já apuradas e não conhece banco,
    /// período de análise nem fonte dos dados.
    /// </summary>
    /// <remarks>
    /// Categorias <see cref="ExpenseNature.Essencial"/> e
    /// <see cref="ExpenseNature.NaoClassificado"/> nunca entram no corte: a primeira
    /// porque o usuário disse que é intocável, a segunda porque cortar um gasto que
    /// o sistema não sabe o que é seria chute. Um usuário que nunca classificou nada
    /// recebe o diagnóstico (sobra, déficit) sem plano de corte — e é a UI que pede
    /// a classificação para destravar o resto.
    /// </remarks>
    public static class SugestaoAporteCalculator
    {
        /// <summary>
        /// Teto de corte por categoria. Mandar zerar uma categoria é conselho que
        /// ninguém segue; o plano fica mais realista distribuindo o aperto.
        /// </summary>
        public const decimal PercentualMaximoCortePorCategoria = 0.40m;

        /// <summary>Gasto médio mensal de uma categoria, com a classificação dada pelo usuário.</summary>
        public record GastoCategoria(Guid CategoryId, string Nome, decimal MediaMensal, ExpenseNature Nature);

        /// <summary>Retrato mensal do orçamento do usuário, já mediado sobre o período analisado.</summary>
        public record PerfilFinanceiroMensal(
            decimal RendaMensal,
            IReadOnlyList<GastoCategoria> Gastos,
            decimal AportesMensaisMedios);

        /// <summary>Quanto o plano sugere cortar de uma categoria específica.</summary>
        public record CorteSugerido(Guid CategoryId, string Nome, decimal GastoAtual, decimal ValorCorte)
        {
            /// <summary>Quanto sobraria para gastar nessa categoria depois do corte.</summary>
            public decimal GastoDepoisDoCorte => GastoAtual - ValorCorte;
        }

        /// <summary>Diagnóstico completo do encaixe da meta no orçamento.</summary>
        public record ResultadoSugestao(
            bool Cabe,
            decimal DespesaTotal,
            decimal SobraLivre,
            decimal SobraTotal,
            decimal Deficit,
            decimal CapacidadeMaxima,
            decimal FolgaRestante,
            decimal PercentualDaRenda,
            IReadOnlyList<CorteSugerido> Cortes,
            IReadOnlyList<GastoCategoria> NaoClassificadas);

        /// <summary>
        /// Calcula se o <paramref name="aporteNecessario"/> cabe no orçamento descrito
        /// por <paramref name="perfil"/> e, se não couber, de onde o dinheiro poderia sair.
        /// </summary>
        /// <param name="perfil">Renda, gastos por categoria e aportes já em curso, todos mensais.</param>
        /// <param name="aporteNecessario">Aporte mensal exigido pela meta.</param>
        public static ResultadoSugestao Calcular(PerfilFinanceiroMensal perfil, decimal aporteNecessario)
        {
            if (perfil is null)
                throw new ArgumentNullException(nameof(perfil));

            if (perfil.RendaMensal < 0)
                throw new ArgumentException("A renda mensal não pode ser negativa.", nameof(perfil));

            if (aporteNecessario < 0)
                throw new ArgumentException("O aporte necessário não pode ser negativo.", nameof(aporteNecessario));

            var gastos = perfil.Gastos ?? Array.Empty<GastoCategoria>();
            var despesaTotal = gastos.Sum(g => g.MediaMensal);

            // Aportes em curso saem da mesma sobra, mas não são despesa: entram à parte
            // para não inflar o gasto e, ao mesmo tempo, não passar a falsa ideia de
            // que esse dinheiro está livre.
            var sobraTotal = perfil.RendaMensal - despesaTotal;
            var sobraLivre = sobraTotal - perfil.AportesMensaisMedios;

            var naoClassificadas = gastos
                .Where(g => g.Nature == ExpenseNature.NaoClassificado && g.MediaMensal > 0)
                .OrderByDescending(g => g.MediaMensal)
                .ToList();

            var percentualDaRenda = perfil.RendaMensal > 0
                ? Math.Round(aporteNecessario / perfil.RendaMensal * 100, 2)
                : 0m;

            var deficit = aporteNecessario - sobraLivre;
            if (deficit <= 0)
            {
                return new ResultadoSugestao(
                    Cabe: true,
                    DespesaTotal: despesaTotal,
                    SobraLivre: sobraLivre,
                    SobraTotal: sobraTotal,
                    Deficit: 0m,
                    CapacidadeMaxima: sobraLivre,
                    FolgaRestante: sobraLivre - aporteNecessario,
                    PercentualDaRenda: percentualDaRenda,
                    Cortes: Array.Empty<CorteSugerido>(),
                    NaoClassificadas: naoClassificadas);
            }

            var cortes = MontarPlanoDeCorte(gastos, deficit);
            var totalCortado = cortes.Sum(c => c.ValorCorte);
            var capacidadeMaxima = sobraLivre + CortePotencialTotal(gastos);

            return new ResultadoSugestao(
                Cabe: totalCortado >= Math.Round(deficit, 2),
                DespesaTotal: despesaTotal,
                SobraLivre: sobraLivre,
                SobraTotal: sobraTotal,
                Deficit: Math.Round(deficit, 2),
                CapacidadeMaxima: Math.Round(capacidadeMaxima, 2),
                FolgaRestante: 0m,
                PercentualDaRenda: percentualDaRenda,
                Cortes: cortes,
                NaoClassificadas: naoClassificadas);
        }

        /// <summary>
        /// Rateia o déficit entre as categorias discricionárias proporcionalmente ao peso
        /// de cada uma, respeitando o teto por categoria. Quem gasta mais contribui mais.
        /// </summary>
        private static IReadOnlyList<CorteSugerido> MontarPlanoDeCorte(
            IReadOnlyList<GastoCategoria> gastos, decimal deficit)
        {
            var discricionarias = gastos
                .Where(g => g.Nature == ExpenseNature.Discricionario && g.MediaMensal > 0)
                .OrderByDescending(g => g.MediaMensal)
                .ToList();

            if (discricionarias.Count == 0)
                return Array.Empty<CorteSugerido>();

            var cortePotencialTotal = CortePotencialTotal(gastos);
            if (cortePotencialTotal <= 0)
                return Array.Empty<CorteSugerido>();

            // Quando nem cortando o teto de todas as categorias o déficit é coberto, o
            // plano vira "corte o máximo possível" — o restante é tratado pelos cenários
            // alternativos (outro prazo, outro alvo), não por um corte impossível.
            var aRatear = Math.Min(deficit, cortePotencialTotal);

            var cortes = new List<CorteSugerido>(discricionarias.Count);
            var acumulado = 0m;

            for (var i = 0; i < discricionarias.Count; i++)
            {
                var categoria = discricionarias[i];
                var tetoCategoria = categoria.MediaMensal * PercentualMaximoCortePorCategoria;

                // A última categoria absorve a diferença de arredondamento, para que a soma
                // dos cortes feche exatamente com o valor rateado.
                var valorCorte = i == discricionarias.Count - 1
                    ? Math.Min(tetoCategoria, aRatear - acumulado)
                    : Math.Round(aRatear * (tetoCategoria / cortePotencialTotal), 2);

                valorCorte = Math.Round(Math.Max(valorCorte, 0m), 2);
                if (valorCorte <= 0)
                    continue;

                acumulado += valorCorte;
                cortes.Add(new CorteSugerido(categoria.CategoryId, categoria.Nome, categoria.MediaMensal, valorCorte));
            }

            return cortes;
        }

        private static decimal CortePotencialTotal(IReadOnlyList<GastoCategoria> gastos) =>
            gastos
                .Where(g => g.Nature == ExpenseNature.Discricionario && g.MediaMensal > 0)
                .Sum(g => g.MediaMensal * PercentualMaximoCortePorCategoria);
    }
}
