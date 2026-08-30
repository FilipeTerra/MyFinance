using Moq;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class RetiradaServiceTests
{
    private readonly Mock<ITaxasReferenciaIntegrationService> _taxasReferenciaService = new();
    private readonly RetiradaService _sut;

    public RetiradaServiceTests()
    {
        _sut = new RetiradaService(_taxasReferenciaService.Object);
    }

    // ---------- CalcularSaqueSustentavelAsync ----------

    [Fact]
    public async Task CalcularSaqueSustentavelAsync_ComputesSaqueAndDoesNotLastForever()
    {
        var request = new CalcularSaqueSustentavelRequestDto
        {
            SaldoInicial = 500000m,
            BaseCustoInicial = 300000m,
            PrazoMeses = 300,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 6m,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        var result = await _sut.CalcularSaqueSustentavelAsync(request);

        Assert.True(result.SaqueMensal > 0);
        Assert.False(result.DuraParaSempre);
        Assert.Equal(300, result.MesEsgotamento);
        Assert.Equal(300, result.Evolucao.Count);
        Assert.True(result.Evolucao[0].ValorImposto > 0); // CDB tributa o ganho do saque
    }

    [Fact]
    public async Task CalcularSaqueSustentavelAsync_WithInvalidBaseCusto_ThrowsArgumentException()
    {
        var request = new CalcularSaqueSustentavelRequestDto
        {
            SaldoInicial = 1000m,
            BaseCustoInicial = 2000m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 6m,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularSaqueSustentavelAsync(request));
    }

    [Fact]
    public async Task CalcularSaqueSustentavelAsync_UsingSelic_FetchesTaxaAndCdi()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync())
            .ReturnsAsync(new TaxasReferenciaDto { SelicAnualPct = 10m });

        var request = new CalcularSaqueSustentavelRequestDto
        {
            SaldoInicial = 500000m,
            BaseCustoInicial = 500000m,
            PrazoMeses = 240,
            FonteTaxaJuros = FonteTaxaJuros.Selic,
            TipoAtivo = TipoAtivoCalculadora.TesouroSelic
        };

        var result = await _sut.CalcularSaqueSustentavelAsync(request);

        Assert.Equal(10m, result.TaxaJurosAnualUtilizada);
        Assert.Null(result.PercentualCdiUtilizado);
    }

    // ---------- CalcularDuracaoAsync ----------

    [Fact]
    public async Task CalcularDuracaoAsync_WhenSustainableForever_ReturnsDuraParaSempreTrue()
    {
        var request = new CalcularDuracaoRetiradaRequestDto
        {
            SaldoInicial = 1_000_000m,
            BaseCustoInicial = 1_000_000m,
            SaqueMensal = 1000m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 12m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularDuracaoAsync(request);

        Assert.True(result.DuraParaSempre);
        Assert.Null(result.MesEsgotamento);
        Assert.Equal(360, result.Evolucao.Count); // janela de exibição, não o "fim" real
    }

    [Fact]
    public async Task CalcularDuracaoAsync_WhenNotSustainable_ReturnsMesEsgotamento()
    {
        var request = new CalcularDuracaoRetiradaRequestDto
        {
            SaldoInicial = 12000m,
            BaseCustoInicial = 12000m,
            SaqueMensal = 1000m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 0m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularDuracaoAsync(request);

        Assert.False(result.DuraParaSempre);
        Assert.Equal(12, result.MesEsgotamento);
        Assert.Equal(12, result.Evolucao.Count);
    }

    [Fact]
    public async Task CalcularDuracaoAsync_WithPercentualCdi_MissingPercentual_ThrowsArgumentException()
    {
        var request = new CalcularDuracaoRetiradaRequestDto
        {
            SaldoInicial = 100000m,
            BaseCustoInicial = 100000m,
            SaqueMensal = 1000m,
            FonteTaxaJuros = FonteTaxaJuros.PercentualCdi,
            PercentualCdi = null,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularDuracaoAsync(request));
    }

    [Fact]
    public async Task CalcularDuracaoAsync_PgblTaxesFullWithdrawal()
    {
        var request = new CalcularDuracaoRetiradaRequestDto
        {
            SaldoInicial = 100000m,
            BaseCustoInicial = 0m,
            SaqueMensal = 2000m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 0m,
            TipoAtivo = TipoAtivoCalculadora.Pgbl
        };

        var result = await _sut.CalcularDuracaoAsync(request);

        Assert.Equal(10m, result.Evolucao[0].AliquotaImpostoPercentual);
        Assert.Equal(200m, result.Evolucao[0].ValorImposto); // 10% sobre o saque inteiro (base de custo zero)
    }
}
