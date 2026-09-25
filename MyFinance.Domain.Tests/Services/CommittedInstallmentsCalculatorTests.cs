using MyFinance.Domain.Services;
using static MyFinance.Domain.Services.CommittedInstallmentsCalculator;

namespace MyFinance.Domain.Tests.Services;

public class CommittedInstallmentsCalculatorTests
{
    private static readonly DateTime Hoje = new(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CategoriaId = Guid.NewGuid();

    private static InstallmentRow Parcela(
        string chave, decimal valor, DateTime data, int numero, int total, string? descricao = null) =>
        new(chave, descricao ?? chave, valor, data, numero, total, CategoriaId, "Compras");

    [Fact]
    public void Calculate_SemParcelas_DevolveVazio()
    {
        var resultado = CommittedInstallmentsCalculator.Calculate(Array.Empty<InstallmentRow>(), Hoje);

        Assert.Equal(0m, resultado.TotalCommitted);
        Assert.Equal(0, resultado.PurchaseCount);
        Assert.Empty(resultado.Purchases);
        Assert.Empty(resultado.Schedule);
    }

    [Fact]
    public void Calculate_UmaCompra_SomaApenasAsParcelasQueFaltam()
    {
        // Parcela 3 de 10 em setembro: faltam 7, de outubro a abril.
        var rows = new[] { Parcela("CENTAURO", 109.99m, new DateTime(2026, 9, 9), 3, 10) };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        Assert.Equal(769.93m, resultado.TotalCommitted);
        Assert.Equal(1, resultado.PurchaseCount);

        var compra = Assert.Single(resultado.Purchases);
        Assert.Equal(7, compra.RemainingInstallments);
        Assert.Equal(109.99m, compra.InstallmentAmount);
        Assert.Equal(2027, compra.LastDueYear);
        Assert.Equal(4, compra.LastDueMonth);
    }

    [Fact]
    public void Calculate_VariasParcelasDaMesmaCompra_UsaAMaisAdiantada()
    {
        // A mesma compra reaparece a cada fatura importada.
        var rows = new[]
        {
            Parcela("CENTAURO", 100m, new DateTime(2026, 7, 9), 1, 4),
            Parcela("CENTAURO", 100m, new DateTime(2026, 8, 9), 2, 4),
            Parcela("CENTAURO", 100m, new DateTime(2026, 9, 9), 3, 4),
        };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        var compra = Assert.Single(resultado.Purchases);
        Assert.Equal(3, compra.CurrentInstallment);
        Assert.Equal(1, compra.RemainingInstallments);
        Assert.Equal(100m, resultado.TotalCommitted);
    }

    [Fact]
    public void Calculate_FaturaAntigaReimportadaForaDeOrdem_NaoFazACompraVoltarNoTempo()
    {
        var rows = new[]
        {
            Parcela("CENTAURO", 100m, new DateTime(2026, 9, 9), 3, 4),
            // Importada depois, mas é de uma fatura anterior.
            Parcela("CENTAURO", 100m, new DateTime(2026, 7, 9), 1, 4),
        };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        Assert.Equal(3, Assert.Single(resultado.Purchases).CurrentInstallment);
    }

    [Fact]
    public void Calculate_CompraQuitada_NaoEntraNoComprometido()
    {
        // "Parcela 3 de 3" é a última: não sobra nada.
        var rows = new[] { Parcela("IRMAOS MATTAR", 80.13m, new DateTime(2026, 5, 26), 3, 3) };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        Assert.Equal(0m, resultado.TotalCommitted);
        Assert.Empty(resultado.Purchases);
    }

    [Fact]
    public void Calculate_CompraCujaUltimaParcelaJaVenceu_NaoEntraNoComprometido()
    {
        // Parcela 1 de 3 em janeiro: a última venceu em março, bem antes de hoje.
        var rows = new[] { Parcela("LOJA ANTIGA", 50m, new DateTime(2026, 1, 10), 1, 3) };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        Assert.Empty(resultado.Purchases);
    }

    [Fact]
    public void Calculate_ComprasDiferentes_SaoAgrupadasSeparadamentePelaChave()
    {
        var rows = new[]
        {
            Parcela("CENTAURO", 100m, new DateTime(2026, 9, 9), 1, 3),
            Parcela("AIRBNB", 200m, new DateTime(2026, 9, 6), 1, 2),
        };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        Assert.Equal(2, resultado.PurchaseCount);
        Assert.Equal(400m, resultado.TotalCommitted);
        // Ordenado pelo que mais pesa.
        Assert.Equal("AIRBNB", resultado.Purchases[0].Description);
    }

    [Fact]
    public void Calculate_Cronograma_DistribuiCadaParcelaNoMesEmQueVence()
    {
        var rows = new[]
        {
            // Faltam 2: out/2026 e nov/2026.
            Parcela("CENTAURO", 100m, new DateTime(2026, 9, 9), 1, 3),
            // Falta 1: out/2026.
            Parcela("AIRBNB", 200m, new DateTime(2026, 9, 6), 1, 2),
        };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        // Out, nov e o mês em que tudo acaba (dez).
        Assert.Equal(3, resultado.Schedule.Count);

        Assert.Equal((2026, 10, 300m, 2), Tupla(resultado.Schedule[0]));
        Assert.Equal((2026, 11, 100m, 1), Tupla(resultado.Schedule[1]));
        Assert.Equal((2026, 12, 0m, 0), Tupla(resultado.Schedule[2]));

        Assert.Equal(300m, resultado.NextMonthAmount);
    }

    [Fact]
    public void Calculate_Cronograma_LiberacaoCresceConformeOsParcelamentosTerminam()
    {
        var rows = new[]
        {
            Parcela("CENTAURO", 100m, new DateTime(2026, 9, 9), 1, 3),
            Parcela("AIRBNB", 200m, new DateTime(2026, 9, 6), 1, 2),
        };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        // Outubro é o patamar de hoje: nada liberado ainda.
        Assert.Equal(0m, resultado.Schedule[0].ReleasedVsNextMonth);
        // Em novembro o AIRBNB já acabou: R$ 200 de volta ao orçamento.
        Assert.Equal(200m, resultado.Schedule[1].ReleasedVsNextMonth);
        // Em dezembro não sobra parcela nenhuma.
        Assert.Equal(300m, resultado.Schedule[2].ReleasedVsNextMonth);
    }

    [Fact]
    public void Calculate_ParcelaLancadaComoDespesaNegativa_ContaPeloValorAbsoluto()
    {
        var rows = new[] { Parcela("CENTAURO", -100m, new DateTime(2026, 9, 9), 1, 3) };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        Assert.Equal(200m, resultado.TotalCommitted);
        Assert.Equal(100m, Assert.Single(resultado.Purchases).InstallmentAmount);
    }

    [Fact]
    public void Calculate_ParcelamentoQueAtravessaOAno_ProjetaOMesCorreto()
    {
        // Parcela 1 de 6 em novembro: a última cai em abril do ano seguinte.
        var rows = new[] { Parcela("LOJA", 50m, new DateTime(2026, 11, 10), 1, 6) };

        var resultado = CommittedInstallmentsCalculator.Calculate(rows, Hoje);

        var compra = Assert.Single(resultado.Purchases);
        Assert.Equal(2027, compra.LastDueYear);
        Assert.Equal(4, compra.LastDueMonth);
    }

    private static (int, int, decimal, int) Tupla(MonthlyCommitment m) =>
        (m.Year, m.Month, m.Amount, m.PurchaseCount);
}
