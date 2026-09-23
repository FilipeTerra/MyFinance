using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Diagnostica a reserva de emergência do usuário: quanto ele já tem guardado
    /// em ativos líquidos frente ao ideal de <see cref="MesesDeRendaIdeais"/> vezes
    /// a renda mensal. Cálculo puro, sem IA — a sugestão de aporte precisa desse
    /// número mesmo com o agente fora do ar.
    /// </summary>
    /// <remarks>
    /// A mesma regra existe hoje em <c>MyFinance.AiAgent/.../Tools/api/proativas.py</c>
    /// (<c>analisar_reserva_emergencia</c>), escrita antes desta. A partir daqui a fonte
    /// de verdade é esta classe: a tool Python deve passar a consumir o resultado da API
    /// .NET, como <c>mercado.py</c> já faz para dados de mercado, em vez de recalcular.
    /// </remarks>
    public static class ReservaEmergenciaCalculator
    {
        /// <summary>Meses de renda que uma reserva de emergência deve cobrir.</summary>
        public const int MesesDeRendaIdeais = 6;

        /// <summary>
        /// Termo procurado no nome das metas para identificar as que são reserva de
        /// emergência. Não existe flag no modelo de dados: a meta é só um nome livre.
        /// </summary>
        private const string TermoMetaReserva = "reserva";

        /// <summary>Uma meta financeira reduzida ao que importa para o diagnóstico.</summary>
        public record MetaGuardada(string Nome, decimal ValorGuardado);

        /// <summary>Diagnóstico da reserva frente ao ideal.</summary>
        public record ResultadoReserva(
            bool Adequada,
            decimal ValorIdeal,
            decimal ValorAtual,
            decimal ValorFaltante,
            decimal PercentualAtingido,
            decimal MesesCobertos);

        /// <summary>
        /// Compara o que o usuário já tem guardado com o ideal de seis meses de renda.
        /// </summary>
        /// <param name="rendaMensal">Renda mensal do usuário. Zero ou negativa torna o diagnóstico impossível.</param>
        /// <param name="metas">Metas financeiras do usuário; só as que têm "reserva" no nome entram na conta.</param>
        /// <param name="valorEmRendaFixa">Total investido em Renda Fixa, tratado como reserva por ser líquido.</param>
        /// <returns><c>null</c> quando não há renda conhecida — sem ela não existe ideal a comparar.</returns>
        public static ResultadoReserva? Calcular(
            decimal rendaMensal,
            IReadOnlyList<MetaGuardada> metas,
            decimal valorEmRendaFixa)
        {
            if (rendaMensal <= 0)
                return null;

            var emMetasDeReserva = (metas ?? Array.Empty<MetaGuardada>())
                .Where(m => (m.Nome ?? string.Empty).Contains(TermoMetaReserva, StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.ValorGuardado);

            var valorIdeal = Math.Round(rendaMensal * MesesDeRendaIdeais, 2);
            var valorAtual = Math.Round(emMetasDeReserva + Math.Max(valorEmRendaFixa, 0m), 2);

            return new ResultadoReserva(
                Adequada: valorAtual >= valorIdeal,
                ValorIdeal: valorIdeal,
                ValorAtual: valorAtual,
                ValorFaltante: Math.Round(Math.Max(valorIdeal - valorAtual, 0m), 2),
                PercentualAtingido: Math.Round(valorAtual / valorIdeal * 100, 2),
                MesesCobertos: Math.Round(valorAtual / rendaMensal, 1));
        }
    }
}
