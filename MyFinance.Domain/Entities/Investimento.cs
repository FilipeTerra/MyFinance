using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Entities
{
    /// <summary>
    /// Representa um investimento pertencente a um usuário (ação, FII, cripto, etc.).
    /// Além do valor aportado inicialmente, mantém o valor atual para acompanhar
    /// a rentabilidade do ativo ao longo do tempo.
    /// </summary>
    public class Investimento
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        /// <summary>
        /// Nome ou identificação do ativo (ex: "Tesouro Selic 2029", "PETR4").
        /// </summary>
        public string Nome { get; private set; }

        /// <summary>
        /// Soma de todo o dinheiro já aportado neste investimento (aporte inicial + aportes
        /// adicionais via <see cref="AdicionarAporte"/>). É a base de custo usada para calcular
        /// a rentabilidade — não é afetada pela variação de mercado.
        /// </summary>
        public decimal TotalAportado { get; private set; }

        /// <summary>
        /// Valor atual de mercado do investimento. Inicia igual ao TotalAportado e é
        /// atualizado via <see cref="AtualizarValorAtual"/> (manual ou sincronização automática).
        /// </summary>
        public decimal ValorAtual { get; private set; }

        /// <summary>
        /// Classe do ativo (Renda Fixa, Ação, FII, Cripto, ETF).
        /// </summary>
        public InvestmentType Tipo { get; private set; }

        /// <summary>
        /// Data de criação/aporte do investimento (UTC).
        /// </summary>
        public DateTime DataCriacao { get; private set; }

        /// <summary>
        /// Código do ativo na B3 (ex: "PETR4"), usado para buscar cotações automaticamente.
        /// Nulo para ativos sem cotação de mercado (ex: Renda Fixa).
        /// </summary>
        public string? Ticker { get; private set; }

        public Investimento(Guid userId, string nome, decimal totalAportado, InvestmentType tipo, string? ticker = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("O investimento precisa pertencer a um usuário.", nameof(userId));

            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("O nome do investimento é obrigatório.", nameof(nome));

            if (totalAportado <= 0)
                throw new ArgumentException("O valor do aporte inicial deve ser maior que zero.", nameof(totalAportado));

            Id = Guid.NewGuid();
            UserId = userId;
            Nome = nome;
            TotalAportado = totalAportado;
            ValorAtual = totalAportado;
            Tipo = tipo;
            DataCriacao = DateTime.UtcNow;
            Ticker = string.IsNullOrWhiteSpace(ticker) ? null : ticker.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Atualiza o valor de mercado do investimento (ex: após reprecificação do ativo).
        /// </summary>
        public void AtualizarValorAtual(decimal novoValor)
        {
            if (novoValor < 0)
                throw new ArgumentException("O valor atual não pode ser negativo.", nameof(novoValor));

            ValorAtual = novoValor;
        }

        /// <summary>
        /// Registra um novo aporte: soma ao total aportado (base de custo) e ao valor atual
        /// — o dinheiro novo entra ao preço de mercado do momento, não é ganho de rentabilidade.
        /// </summary>
        public void AdicionarAporte(decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("O valor do aporte deve ser maior que zero.", nameof(valor));

            TotalAportado += valor;
            ValorAtual += valor;
        }

        /// <summary>
        /// Rentabilidade percentual do investimento em relação ao total aportado.
        /// </summary>
        public decimal RentabilidadePercentual =>
            TotalAportado == 0 ? 0 : ((ValorAtual - TotalAportado) / TotalAportado) * 100;
    }
}
