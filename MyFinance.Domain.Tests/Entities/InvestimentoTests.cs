using MyFinance.Domain.Entities;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Tests.Entities;

public class InvestimentoTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesInvestimentoWithValorAtualEqualToValorInicial()
    {
        var investimento = new Investimento(ValidUserId, "Tesouro Selic 2029", 1000m, InvestmentType.RendaFixa);

        Assert.NotEqual(Guid.Empty, investimento.Id);
        Assert.Equal(ValidUserId, investimento.UserId);
        Assert.Equal("Tesouro Selic 2029", investimento.Nome);
        Assert.Equal(1000m, investimento.ValorInicial);
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
    public void AtualizarValorAtual_DoesNotAffectValorInicial()
    {
        var investimento = new Investimento(ValidUserId, "PETR4", 100m, InvestmentType.Acao);

        investimento.AtualizarValorAtual(150m);

        Assert.Equal(100m, investimento.ValorInicial);
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
}
