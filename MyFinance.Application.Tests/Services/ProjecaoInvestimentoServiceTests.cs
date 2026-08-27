using Moq;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Dtos.Mercado;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;
using MyFinance.Domain.Enums;

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
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(12.0m, result.TaxaJurosAnualUtilizada);
        Assert.Equal(57794.05m, result.ValorFinal, 1);
        Assert.Null(result.IpcaAnualUtilizado);
        Assert.Null(result.RentabilidadeRealAnualPercentual);
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
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularProjecaoAsync(request));
    }

    [Fact]
    public async Task CalcularProjecaoAsync_UsingSelic_WithSuccessfulLookup_UsesReturnedRate()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync())
            .ReturnsAsync(new TaxasReferenciaDto { SelicAnualPct = 10.75m, IpcaAnualPct = 4.5m });
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 120,
            FonteTaxaJuros = FonteTaxaJuros.Selic,
            TipoAtivo = TipoAtivoCalculadora.TesouroSelic
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(10.75m, result.TaxaJurosAnualUtilizada);
        Assert.Equal(4.5m, result.IpcaAnualUtilizado);
        Assert.NotNull(result.RentabilidadeRealAnualPercentual);
        Assert.Null(result.PercentualCdiUtilizado);
        Assert.Null(result.CdiAnualUtilizado);
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
            FonteTaxaJuros = FonteTaxaJuros.Selic,
            TipoAtivo = TipoAtivoCalculadora.TesouroSelic
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CalcularProjecaoAsync(request));
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithCdb_AppliesRegressiveIncomeTaxOnEarnings()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.RendaFixaTributavel, result.CategoriaTributaria);
        Assert.Equal(20m, result.AliquotaImpostoRendaPercentual);
        Assert.True(result.ValorImpostoRenda > 0);
        Assert.Equal(result.ValorFinal - result.ValorImpostoRenda, result.ValorFinalLiquido, 2);
        // Prazo mínimo simulável (1 mês ≈ 30 dias) já cai fora da faixa de IOF (só incide < 30 dias).
        Assert.Equal(0m, result.ValorIof);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithLci_ReturnsZeroTaxAndCategoriaIsenta()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.RendaFixaIsenta, result.CategoriaTributaria);
        Assert.Equal(0m, result.ValorIof);
        Assert.Equal(0m, result.ValorImpostoRenda);
        Assert.Equal(result.ValorFinal, result.ValorFinalLiquido);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithAcao_AppliesGanhoCapitalNotIof()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 50000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Acao
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.GanhoCapitalAcao, result.CategoriaTributaria);
        Assert.Equal(0m, result.ValorIof);
        Assert.Equal(15m, result.AliquotaImpostoRendaPercentual);
        Assert.False(result.IsentoPorFaixaDeVenda);
        Assert.True(result.ValorImpostoRenda > 0);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithAcao_BelowVendaThreshold_IsIsento()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Acao
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(0m, result.AliquotaImpostoRendaPercentual);
        Assert.Equal(0m, result.ValorImpostoRenda);
        Assert.True(result.IsentoPorFaixaDeVenda);
        Assert.Equal(result.ValorFinal, result.ValorFinalLiquido);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithFii_NoIsencaoThreshold_AlwaysTaxed()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Fii
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.GanhoCapitalFii, result.CategoriaTributaria);
        Assert.Equal(20m, result.AliquotaImpostoRendaPercentual);
        Assert.False(result.IsentoPorFaixaDeVenda);
        Assert.True(result.ValorImpostoRenda > 0);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithCripto_AboveThreshold_AppliesFifteenPercent()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 60000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Cripto
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.GanhoCapitalCripto, result.CategoriaTributaria);
        Assert.Equal(15m, result.AliquotaImpostoRendaPercentual);
        Assert.False(result.IsentoPorFaixaDeVenda);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithCripto_BelowThreshold_IsIsento()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Cripto
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.True(result.IsentoPorFaixaDeVenda);
        Assert.Equal(0m, result.ValorImpostoRenda);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithFundoAcoes_AppliesFlatFifteenPercent_NoIsencao()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.FundoAcoes
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.GanhoCapitalFundoAcoes, result.CategoriaTributaria);
        Assert.Equal(15m, result.AliquotaImpostoRendaPercentual);
        Assert.False(result.IsentoPorFaixaDeVenda);
        Assert.True(result.ValorImpostoRenda > 0);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithFundoRendaFixaLongoPrazo_WithholdsComeCotasAndComplementaAtRedemption()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 200m,
            PrazoMeses = 24,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.FundoRendaFixaLongoPrazo
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.FundoComeCotasLongoPrazo, result.CategoriaTributaria);
        Assert.Equal(15m, result.AliquotaComeCotasPercentual);
        Assert.True(result.ValorComeCotasRetido > 0);
        Assert.Equal(0m, result.ValorIof);
        Assert.Equal(24, result.Evolucao.Count);
        Assert.Equal(result.ValorFinal - result.ValorImpostoRenda, result.ValorFinalLiquido, 2);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithFundoRendaFixaCurtoPrazo_UsesTwentyPercentAntecipacao()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.FundoRendaFixaCurtoPrazo
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.FundoComeCotasCurtoPrazo, result.CategoriaTributaria);
        Assert.Equal(20m, result.AliquotaComeCotasPercentual);
        Assert.True(result.ValorComeCotasRetido > 0);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithPgbl_TaxesFullWithdrawalAmount()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 24,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Pgbl
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.PrevidenciaPgbl, result.CategoriaTributaria);
        Assert.Equal(35m, result.AliquotaImpostoRendaPercentual);
        Assert.Equal(Math.Round(result.ValorFinal * 0.35m, 2), result.ValorImpostoRenda);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithVgbl_TaxesOnlyEarnings()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 24,
            TaxaJurosAnualPercentual = 12.0m,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TipoAtivo = TipoAtivoCalculadora.Vgbl
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(CategoriaTributariaAtivo.PrevidenciaVgbl, result.CategoriaTributaria);
        Assert.Equal(35m, result.AliquotaImpostoRendaPercentual);
        Assert.Equal(Math.Round(result.TotalJuros * 0.35m, 2), result.ValorImpostoRenda);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithPercentualCdi_ComputesEffectiveRateFromCdi()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync())
            .ReturnsAsync(new TaxasReferenciaDto { CdiAnualPct = 10m, IpcaAnualPct = 4m });
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.PercentualCdi,
            PercentualCdi = 110m,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(11m, result.TaxaJurosAnualUtilizada); // 110% de 10%
        Assert.Equal(110m, result.PercentualCdiUtilizado);
        Assert.Equal(10m, result.CdiAnualUtilizado);
        Assert.Equal(4m, result.IpcaAnualUtilizado);
        Assert.NotNull(result.RentabilidadeRealAnualPercentual);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithPercentualCdi_MissingPercentual_ThrowsArgumentException()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.PercentualCdi,
            PercentualCdi = null,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularProjecaoAsync(request));
        _taxasReferenciaService.Verify(s => s.GetTaxasReferenciaAsync(), Times.Never);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithPercentualCdi_WithFailedLookup_ThrowsInvalidOperationException()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync()).ReturnsAsync((TaxasReferenciaDto?)null);
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 10000m,
            AporteMensal = 0m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.PercentualCdi,
            PercentualCdi = 100m,
            TipoAtivo = TipoAtivoCalculadora.Cdb
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CalcularProjecaoAsync(request));
    }

    // ---------- Aportes extras ----------

    [Fact]
    public async Task CalcularProjecaoAsync_WithAporteExtra_IncreasesTotalAportado()
    {
        var baseRequest = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 100m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        var semExtra = await _sut.CalcularProjecaoAsync(baseRequest);
        var comExtra = await _sut.CalcularProjecaoAsync(baseRequest with
        {
            AportesExtras = new[] { new AporteExtraDto { Mes = 6, Valor = 2000m } }
        });

        Assert.Equal(semExtra.TotalAportado + 2000m, comExtra.TotalAportado);
        Assert.True(comExtra.ValorFinal > semExtra.ValorFinal);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithAporteExtra_AppliesToFundoComeCotas()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 1000m,
            AporteMensal = 100m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.FundoRendaFixaLongoPrazo,
            AportesExtras = new[] { new AporteExtraDto { Mes = 3, Valor = 5000m } }
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(1000m + 100m * 12 + 5000m, result.TotalAportado);
    }

    // ---------- Reajuste do aporte mensal ----------

    [Fact]
    public async Task CalcularProjecaoAsync_WithReajustePercentualFixo_IncreasesAporteAnnually()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 24,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 0m,
            TipoAtivo = TipoAtivoCalculadora.Lci,
            ReajusteAporteModo = ReajusteAporteModo.PercentualFixo,
            ReajusteAporteAnualPercentual = 10m
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        // 12 meses a 1000 + 12 meses a 1100 (sem juros, taxa 0%).
        Assert.Equal(1000m * 12 + 1100m * 12, result.TotalAportado);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithReajustePercentualFixo_MissingPercentual_ThrowsArgumentException()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 24,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci,
            ReajusteAporteModo = ReajusteAporteModo.PercentualFixo,
            ReajusteAporteAnualPercentual = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CalcularProjecaoAsync(request));
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithReajusteIpca_FetchesIpcaEvenInManualTaxaMode()
    {
        _taxasReferenciaService.Setup(s => s.GetTaxasReferenciaAsync())
            .ReturnsAsync(new TaxasReferenciaDto { IpcaAnualPct = 5m });

        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 24,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci,
            ReajusteAporteModo = ReajusteAporteModo.Ipca
        };

        var result = await _sut.CalcularProjecaoAsync(request);

        Assert.Equal(5m, result.IpcaAnualUtilizado);
        Assert.Equal(1000m * 12 + 1050m * 12, result.TotalAportado);
        _taxasReferenciaService.Verify(s => s.GetTaxasReferenciaAsync(), Times.Once);
    }

    [Fact]
    public async Task CalcularProjecaoAsync_WithoutReajuste_DoesNotFetchTaxasInManualMode()
    {
        var request = new CalcularProjecaoRequestDto
        {
            AporteInicial = 0m,
            AporteMensal = 1000m,
            PrazoMeses = 12,
            FonteTaxaJuros = FonteTaxaJuros.Manual,
            TaxaJurosAnualPercentual = 10m,
            TipoAtivo = TipoAtivoCalculadora.Lci
        };

        await _sut.CalcularProjecaoAsync(request);

        _taxasReferenciaService.Verify(s => s.GetTaxasReferenciaAsync(), Times.Never);
    }
}
