namespace MyFinance.Application.Dtos.Investimentos
{
    /// <summary>Aporte avulso (ex.: 13º salário, bônus) somado ao aporte mensal recorrente em um mês específico.</summary>
    public record AporteExtraDto
    {
        public int Mes { get; init; }
        public decimal Valor { get; init; }
    }
}
