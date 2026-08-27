namespace MyFinance.Domain.Services
{
    /// <summary>Aporte avulso (ex.: 13º salário, bônus) somado ao aporte mensal recorrente em um mês específico.</summary>
    public record AporteExtra(int Mes, decimal Valor);
}
