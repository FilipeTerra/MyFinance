namespace MyFinance.Domain.Enums
{
    /// <summary>
    /// Quão sério é um aviso do simulador de financiamento. Nunca bloqueia a
    /// simulação — é sempre informativo, nunca motivo de <c>ArgumentException</c>.
    /// </summary>
    public enum SeveridadeAvisoFinanciamento
    {
        /// <summary>Contexto útil, sem risco — ex.: qual sistema o programa usa por padrão.</summary>
        Informativo = 1,

        /// <summary>O usuário deveria conferir algo antes de seguir — ex.: parcela pesada, teto de valor.</summary>
        Atencao = 2,

        /// <summary>A simulação foge de uma regra dura do programa/contrato, mesmo sem impedir o cálculo.</summary>
        Critico = 3
    }
}
