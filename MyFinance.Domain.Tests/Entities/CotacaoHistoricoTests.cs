using MyFinance.Domain.Entities;

namespace MyFinance.Domain.Tests.Entities;

public class CotacaoHistoricoTests
{
    private static readonly Guid ValidInvestimentoId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesCotacaoHistorico()
    {
        var data = new DateTime(2026, 5, 1);

        var cotacao = new CotacaoHistorico(ValidInvestimentoId, data, 42.5m);

        Assert.NotEqual(Guid.Empty, cotacao.Id);
        Assert.Equal(ValidInvestimentoId, cotacao.InvestimentoId);
        Assert.Equal(data, cotacao.Data);
        Assert.Equal(42.5m, cotacao.Valor);
    }

    [Fact]
    public void Constructor_WithEmptyInvestimentoId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new CotacaoHistorico(Guid.Empty, DateTime.UtcNow, 10m));
    }

    [Fact]
    public void Constructor_WithNegativeValor_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new CotacaoHistorico(ValidInvestimentoId, DateTime.UtcNow, -1m));
    }

    [Fact]
    public void Constructor_WithZeroValor_IsAllowed()
    {
        var cotacao = new CotacaoHistorico(ValidInvestimentoId, DateTime.UtcNow, 0m);

        Assert.Equal(0m, cotacao.Valor);
    }
}
