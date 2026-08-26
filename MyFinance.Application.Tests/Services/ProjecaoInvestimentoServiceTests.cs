using Moq;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;

namespace MyFinance.Application.Tests.Services;

public class ProjecaoInvestimentoServiceTests
{
    private readonly Mock<ITaxasReferenciaIntegrationService> _taxasReferenciaService = new();
    private readonly ProjecaoInvestimentoService _sut;

    public ProjecaoInvestimentoServiceTests()
    {
        _sut = new ProjecaoInvestimentoService(_taxasReferenciaService.Object);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithManualTaxa_CalculatesWithoutQueryingSelic()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 500m,
            PrazoMeses = 60,
            TaxaJurosAnualPercentual = 12.0m,
            UsarTaxaSelic = false
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(12.0m, result.TaxaJurosAnualUtilizada);
        Assert.Equal(57794.05m, result.ValorFinal, 1);
        _taxasReferenciaService.Verify(s => s.GetTaxasReferenciaAsync(), Times.Never);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithoutManualTaxaAndNotUsingSelic_ThrowsArgumentException()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 100m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = null,
            UsarTaxaSelic = false
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularProjecaoAsync(request));
    }

    [Fact]
    public async Task CalcularProjecaoAsync_UsingSelic_WithSuccessfulLookup_UsesReturnedRate()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync())
            .ReturnsAsync(new TaxasReferenciaDto { SelicAnualPct = 10.75m });
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 120,
            UsarTaxaSelic = true
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(10.75m, result.TaxaJurosAnualUtilizada);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_UsingSelic_WithFailedLookup_ThrowsInvalidOperationException()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync()).ReturnsAsync((TaxasReferenciaDto?)null);
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 120,
            UsarTaxaSelic = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CalcularProjecaoAsync(request));
    }
}
