using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Tests.Entities;

public class InvestimentoTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesInvestimentoWithValorAtualEqualToTotalAportado()
    {
        var investimento = new Investimento(ValidUserId, "Tesouro Selic 2029", 1000m, InvestmentType.RendaFixa);

        Assert.NotEqual(Guid.Empty, investimento.Id);
        Assert.Equal(ValidUserId, investimento.UserId);
        Assert.Equal("Tesouro Selic 2029", investimento.Nome);
        Assert.Equal(1000m, investimento.TotalAportado);
        Assert.Equal(1000m, investimento.ValorAtual);
        Assert.Equal(InvestmentType.RendaFixa, investimento.Tipo);
        Assert.True(investimento.DataCriacao <= DateTime.UtcNow && investimento.DataCriacao > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Investimento(Guid.Empty, "PETR4", 100m, InvestmentType.Acao));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidNome_ThrowsArgumentException(string? invalidNome)
    {
        Assert.Throws<ArgumentException>(() =>
            new Investimento(ValidUserId, invalidNome!, 100m, InvestmentType.Acao));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Constructor_WithNonPositiveValorInicial_ThrowsArgumentException(double invalidValor)
    {
        Assert.Throws<ArgumentException>(() =>
            new Investimento(ValidUserId, "PETR4", (decimal)invalidValor, InvestmentType.Acao));
    }

    [Fact]
    public void AtualizarValorAtual_WithValidValue_UpdatesValorAtual()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AtualizarValorAtual(150m);

        Assert.Equal(150m, investimento.ValorAtual);
    }

    [Fact]
    public void AtualizarValorAtual_DoesNotAffectTotalAportado()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AtualizarValorAtual(150m);

        Assert.Equal(100m, investimento.TotalAportado);
    }

    [Fact]
    public void AtualizarValorAtual_WithZero_IsAllowed()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AtualizarValorAtual(0m);

        Assert.Equal(0m, investimento.ValorAtual);
    }

    [Fact]
    public void AtualizarValorAtual_WithNegativeValue_ThrowsArgumentException()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        Assert.Throws<ArgumentException>(() => investimento.AtualizarValorAtual(-1m));
    }

    [Fact]
    public void RentabilidadePercentual_WithGain_ReturnsPositivePercentage()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AtualizarValorAtual(120m);

        Assert.Equal(20m, investimento.RentabilidadePercentual);
    }

    [Fact]
    public void RentabilidadePercentual_WithLoss_ReturnsNegativePercentage()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AtualizarValorAtual(80m);

        Assert.Equal(-20m, investimento.RentabilidadePercentual);
    }

    [Fact]
    public void RentabilidadePercentual_WithNoChange_ReturnsZero()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        Assert.Equal(0m, investimento.RentabilidadePercentual);
    }

    [Fact]
    public void Constructor_WithTicker_NormalizesToUpperCaseTrimmed()
    {
        var investimento = new Investimento(ValidUserId, "Petrobras", 100m, InvestmentType.Acao, "  petr4  ");

        Assert.Equal("PETR4", investimento.Ticker);
    }

    [Fact]
    public void Constructor_WithoutTicker_TickerIsNull()
    {
        var investimento = new Investimento(ValidUserId, "Tesouro Selic 2029", 100m, InvestmentType.RendaFixa);

        Assert.Null(investimento.Ticker);
    }

    [Fact]
    public void AdicionarAporte_WithValidValue_IncreasesBothTotalAportadoAndValorAtual()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AdicionarAporte(50m);

        Assert.Equal(150m, investimento.TotalAportado);
        Assert.Equal(150m, investimento.ValorAtual);
    }

    [Fact]
    public void AdicionarAporte_AfterMarketUpdate_AccumulatesOnTopOfCurrentValues()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);
        investimento.AtualizarValorAtual(120m);

        investimento.AdicionarAporte(80m);

        Assert.Equal(180m, investimento.TotalAportado);
        Assert.Equal(200m, investimento.ValorAtual);
    }

    [Fact]
    public void AdicionarAporte_DoesNotChangeGanhoAbsoluto()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);
        investimento.AtualizarValorAtual(120m); // ganho de 20 até aqui

        investimento.AdicionarAporte(80m); // dinheiro novo, não é ganho de mercado

        Assert.Equal(20m, investimento.ValorAtual - investimento.TotalAportado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AdicionarAporte_WithNonPositiveValue_ThrowsArgumentException(decimal invalidValor)
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        Assert.Throws<ArgumentException>(() => investimento.AdicionarAporte(invalidValor));
    }
}
