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
        /// Valor aportado no momento da criação do investimento.
        /// </summary>
        public decimal ValorInicial { get; private set; }

        /// <summary>
        /// Valor atual de mercado do investimento. Inicia igual ao ValorInicial
        /// e é atualizado via <see cref="AtualizarValorAtual"/>.
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

        public Investimento(Guid userId, string nome, decimal valorInicial, InvestmentType tipo)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("O investimento precisa pertencer a um usuário.", nameof(userId));

            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("O nome do investimento é obrigatório.", nameof(nome));

            if (valorInicial <= 0)
                throw new ArgumentException("O valor inicial deve ser maior que zero.", nameof(valorInicial));

            Id = Guid.NewGuid();
            UserId = userId;
            Nome = nome;
            ValorInicial = valorInicial;
            ValorAtual = valorInicial;
            Tipo = tipo;
            DataCriacao = DateTime.UtcNow;
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
        /// Rentabilidade percentual do investimento em relação ao valor aportado.
        /// </summary>
        public decimal RentabilidadePercentual =>
            ValorInicial == 0 ? 0 : ((ValorAtual - ValorInicial) / ValorInicial) * 100;
    }
}
