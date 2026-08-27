using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Calcula o Imposto de Renda de planos de previdência privada (PGBL/VGBL) no
    /// regime regressivo definitivo — tabela própria, diferente da renda fixa.
    /// Não suporta o regime progressivo, cuja alíquota depende da renda anual total
    /// do titular e foge do escopo desta calculadora.
    /// </summary>
    public static class PrevidenciaCalculator
    {
        public record ResultadoPrevidencia(decimal AliquotaPercentual, decimal ValorImposto, decimal ValorLiquido);

        /// <summary>
        /// Alíquota da tabela regressiva de previdência, por tempo de acumulação:
        /// até 2 anos: 35% · até 4 anos: 30% · até 6 anos: 25% · até 8 anos: 20% ·
        /// até 10 anos: 15% · acima de 10 anos: 10%.
        /// </summary>
        public static decimal ObterAliquotaRegressiva(int prazoMeses)
        {
            if (prazoMeses <= 24) return 35m;
            if (prazoMeses <= 48) return 30m;
            if (prazoMeses <= 72) return 25m;
            if (prazoMeses <= 96) return 20m;
            if (prazoMeses <= 120) return 15m;
            return 10m;
        }

        /// <summary>
        /// No PGBL, o IR incide sobre o valor total resgatado (as contribuições
        /// foram deduzidas do IR na época do aporte). No VGBL, incide apenas
        /// sobre o rendimento, como na renda fixa.
        /// </summary>
        public static ResultadoPrevidencia Calcular(decimal totalJuros, decimal valorFinal, int prazoMeses, CategoriaTributariaAtivo categoria)
        {
            if (totalJuros <= 0)
                return new ResultadoPrevidencia(0m, 0m, valorFinal);

            if (categoria != CategoriaTributariaAtivo.PrevidenciaPgbl && categoria != CategoriaTributariaAtivo.PrevidenciaVgbl)
                throw new ArgumentException("Categoria não é de previdência privada.", nameof(categoria));

            var aliquota = ObterAliquotaRegressiva(prazoMeses);
            var baseTributavel = categoria == CategoriaTributariaAtivo.PrevidenciaPgbl ? valorFinal : totalJuros;
            var valorImposto = Math.Round(baseTributavel * aliquota / 100, 2);

            return new ResultadoPrevidencia(aliquota, valorImposto, valorFinal - valorImposto);
        }
    }
}
