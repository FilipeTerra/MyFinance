using MyFinance.Application.Dtos.Financiamento;
using MyFinance.Application.Services;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Tests.Services;

public class FinanciamentoServiceTests
{
    private readonly FinanciamentoService _sut = new();

    private static FinanciamentoRequestDto Pedido(
        decimal? valorImovel = null,
        decimal? entrada = null,
        decimal? entradaPercentual = null,
        decimal valorFinanciado = 200000m,
        decimal taxaMensal = 0.8m,
        int numParcelas = 240,
        decimal extraMensal = 0m,
        List<AmortizacaoExtraAvulsaDto>? avulsas = null,
        ModoAmortizacaoExtra modo = ModoAmortizacaoExtra.ReduzirPrazo,
        decimal mip = 0m,
        decimal dfi = 0m,
        decimal taxaAdministracao = 0m,
        decimal tarifasContratacao = 0m) =>
        new()
        {
            SeguroMipMensalPercentualSaldo = mip,
            SeguroDfiMensalPercentualImovel = dfi,
            TaxaAdministracaoMensal = taxaAdministracao,
            TarifasContratacao = tarifasContratacao,
            ValorImovel = valorImovel,
            Entrada = entrada,
            EntradaPercentual = entradaPercentual,
            ValorFinanciado = valorFinanciado,
            TaxaJurosMensalPercentual = taxaMensal,
            NumParcelas = numParcelas,
            AmortizacaoExtraMensal = extraMensal,
            AmortizacoesExtrasAvulsas = avulsas,
            ModoAmortizacaoExtra = modo
        };

    // ---------- Entrada ----------

    [Fact]
    public async Task SimularAsync_ComValorDoImovelEEntradaEmReais_FinanciaADiferenca()
    {
        var resultado = await _sut.SimularAsync(Pedido(valorImovel: 500000m, entrada: 100000m));

        Assert.Equal(500000m, resultado.Composicao.ValorImovel);
        Assert.Equal(100000m, resultado.Composicao.Entrada);
        Assert.Equal(400000m, resultado.Composicao.ValorFinanciado);
        Assert.Equal(20m, resultado.Composicao.EntradaPercentual);
    }

    [Fact]
    public async Task SimularAsync_ComEntradaEmPercentual_ConverteParaReais()
    {
        var resultado = await _sut.SimularAsync(Pedido(valorImovel: 500000m, entradaPercentual: 30m));

        Assert.Equal(150000m, resultado.Composicao.Entrada);
        Assert.Equal(350000m, resultado.Composicao.ValorFinanciado);
    }

    [Fact]
    public async Task SimularAsync_SemValorDoImovel_MantemOContratoAntigo()
    {
        // Compatibilidade: quem já mandava só o valor financiado continua funcionando.
        var resultado = await _sut.SimularAsync(Pedido(valorFinanciado: 200000m));

        Assert.Equal(200000m, resultado.Composicao.ValorFinanciado);
        Assert.Equal(0m, resultado.Composicao.Entrada);
    }

    [Fact]
    public async Task SimularAsync_ComEntradaMaiorQueOImovel_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SimularAsync(Pedido(valorImovel: 300000m, entrada: 300000m)));
    }

    [Fact]
    public async Task SimularAsync_AEntradaReduzOsJurosPagos()
    {
        var semEntrada = await _sut.SimularAsync(Pedido(valorImovel: 500000m));
        var comEntrada = await _sut.SimularAsync(Pedido(valorImovel: 500000m, entrada: 100000m));

        Assert.True(comEntrada.Sac.TotalJuros < semEntrada.Sac.TotalJuros);
    }

    // ---------- Amortização extra ----------

    [Fact]
    public async Task SimularAsync_SemAmortizacaoExtra_NaoRelataEconomia()
    {
        var resultado = await _sut.SimularAsync(Pedido());

        Assert.Equal(0m, resultado.Sac.EconomiaJuros);
        Assert.Equal(0, resultado.Sac.MesesEconomizados);
        Assert.Equal(0m, resultado.Sac.TotalAmortizacaoExtra);
        Assert.Equal(240, resultado.Sac.PrazoFinalMeses);
    }

    [Fact]
    public async Task SimularAsync_ComExtraMensalReduzindoPrazo_RelataEconomiaEmJurosEEmMeses()
    {
        var resultado = await _sut.SimularAsync(Pedido(extraMensal: 2000m));

        Assert.True(resultado.Sac.EconomiaJuros > 0);
        Assert.True(resultado.Sac.MesesEconomizados > 0);
        Assert.Equal(240 - resultado.Sac.MesesEconomizados, resultado.Sac.PrazoFinalMeses);
    }

    [Fact]
    public async Task SimularAsync_ComAmortizacaoAvulsa_AbateNoMesIndicado()
    {
        var avulsas = new List<AmortizacaoExtraAvulsaDto> { new() { Mes = 24, Valor = 30000m } };

        var resultado = await _sut.SimularAsync(Pedido(avulsas: avulsas));

        Assert.Equal(30000m, resultado.Sac.TotalAmortizacaoExtra);
        Assert.Equal(30000m, resultado.Sac.Parcelas[23].AmortizacaoExtra);
        Assert.True(resultado.Sac.EconomiaJuros > 0);
    }

    [Fact]
    public async Task SimularAsync_ReduzirParcela_NaoAntecipaQuitacaoComAporteUnico()
    {
        var avulsas = new List<AmortizacaoExtraAvulsaDto> { new() { Mes = 24, Valor = 30000m } };

        var resultado = await _sut.SimularAsync(
            Pedido(avulsas: avulsas, modo: ModoAmortizacaoExtra.ReduzirParcela));

        Assert.Equal(240, resultado.Sac.PrazoFinalMeses);
        Assert.Equal(0, resultado.Sac.MesesEconomizados);
        Assert.True(resultado.Sac.EconomiaJuros > 0);
    }

    [Fact]
    public async Task SimularAsync_ComAmortizacaoAvulsaForaDoPrazo_ThrowsArgumentException()
    {
        var avulsas = new List<AmortizacaoExtraAvulsaDto> { new() { Mes = 999, Valor = 1000m } };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(Pedido(avulsas: avulsas)));
    }

    [Fact]
    public async Task SimularAsync_ParcelaTotalSomaAParcelaContratualMaisOExtra()
    {
        var resultado = await _sut.SimularAsync(Pedido(extraMensal: 2000m));

        var primeira = resultado.Sac.Parcelas[0];
        Assert.Equal(primeira.ValorParcela + primeira.AmortizacaoExtra, primeira.ParcelaTotal);
        Assert.Equal(2000m, primeira.AmortizacaoExtra);
    }

    // ---------- Seguros e tarifas ----------

    [Fact]
    public async Task SimularAsync_SemEncargos_NaoCobraNadaAlemDasParcelas()
    {
        var resultado = await _sut.SimularAsync(Pedido());

        Assert.Equal(0m, resultado.Sac.TotalSeguros);
        Assert.Equal(0m, resultado.Sac.TotalTaxaAdministracao);
        Assert.Equal(resultado.Sac.TotalPago, resultado.Sac.TotalDesembolsado);
    }

    [Fact]
    public async Task SimularAsync_ComEncargos_AumentaODesembolsoSemMexerNaDivida()
    {
        var sem = await _sut.SimularAsync(Pedido(valorImovel: 500000m, entrada: 100000m));
        var com = await _sut.SimularAsync(Pedido(
            valorImovel: 500000m, entrada: 100000m, mip: 0.025m, dfi: 0.01m, taxaAdministracao: 25m));

        Assert.True(com.Sac.TotalDesembolsado > sem.Sac.TotalDesembolsado);
        // Seguro é despesa, não dívida: os juros do contrato não mudam.
        Assert.Equal(sem.Sac.TotalJuros, com.Sac.TotalJuros);
        Assert.Equal(sem.Sac.TotalPago, com.Sac.TotalPago);
    }

    [Fact]
    public async Task SimularAsync_ComEncargos_PrimeiraParcelaTotalEMaiorQueAContratual()
    {
        var resultado = await _sut.SimularAsync(Pedido(
            valorImovel: 500000m, entrada: 100000m, mip: 0.025m, dfi: 0.01m, taxaAdministracao: 25m));

        var primeira = resultado.Sac.Parcelas[0];
        Assert.True(resultado.Sac.PrimeiraParcelaTotal > resultado.Sac.PrimeiraParcela);
        Assert.Equal(primeira.ParcelaTotal, resultado.Sac.PrimeiraParcelaTotal);
        // MIP: 0,025% de 400.000 = 100. DFI: 0,01% de 500.000 = 50.
        Assert.Equal(100m, primeira.SeguroMip);
        Assert.Equal(50m, primeira.SeguroDfi);
        Assert.Equal(25m, primeira.TaxaAdministracao);
    }

    [Fact]
    public async Task SimularAsync_SemValorDoImovel_CobraDfiSobreOFinanciado()
    {
        // Sem entrada informada, o financiamento é de 100% do bem — então o valor
        // do imóvel é o próprio financiado, e é essa a base correta do DFI.
        var resultado = await _sut.SimularAsync(Pedido(valorFinanciado: 200000m, dfi: 0.01m));

        Assert.Equal(200000m, resultado.Composicao.ValorImovel);
        Assert.All(resultado.Sac.Parcelas, p => Assert.Equal(20m, p.SeguroDfi));
    }

    [Fact]
    public async Task SimularAsync_ComEncargoNegativo_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(Pedido(mip: -0.01m)));
    }

    [Fact]
    public async Task SimularAsync_EncargosNaoInterferemNaEconomiaDaAmortizacaoExtra()
    {
        // A economia compara contratos com os mesmos encargos — o seguro não pode
        // aparecer como se fosse juros economizados.
        var semEncargos = await _sut.SimularAsync(Pedido(extraMensal: 2000m));
        var comEncargos = await _sut.SimularAsync(Pedido(extraMensal: 2000m, mip: 0.025m, taxaAdministracao: 25m));

        Assert.Equal(semEncargos.Sac.EconomiaJuros, comEncargos.Sac.EconomiaJuros);
        Assert.Equal(semEncargos.Sac.MesesEconomizados, comEncargos.Sac.MesesEconomizados);
    }

    // ---------- CET ----------

    [Fact]
    public async Task SimularAsync_SemEncargosNemTarifas_CetIgualaATaxaDeContrato()
    {
        var resultado = await _sut.SimularAsync(Pedido());

        Assert.True(resultado.Sac.CetConvergiu);
        Assert.Equal(0.8m, resultado.Sac.CetMensalPercentual, 2);
    }

    [Fact]
    public async Task SimularAsync_ComEncargos_CetFicaAcimaDaTaxaDeContrato()
    {
        var resultado = await _sut.SimularAsync(Pedido(mip: 0.03m, dfi: 0.01m, taxaAdministracao: 30m));

        Assert.True(resultado.Sac.CetConvergiu);
        Assert.True(resultado.Sac.CetMensalPercentual > 0.8m);
    }

    [Fact]
    public async Task SimularAsync_ComTarifaDeContratacao_AumentaOCet()
    {
        var sem = await _sut.SimularAsync(Pedido());
        var com = await _sut.SimularAsync(Pedido(tarifasContratacao: 5000m));

        Assert.True(com.Sac.CetMensalPercentual > sem.Sac.CetMensalPercentual);
    }

    [Fact]
    public async Task SimularAsync_ComTarifaMaiorQueOFinanciado_NaoConverge()
    {
        var resultado = await _sut.SimularAsync(Pedido(valorFinanciado: 1000m, tarifasContratacao: 1000m));

        Assert.False(resultado.Sac.CetConvergiu);
        Assert.Equal(0m, resultado.Sac.CetMensalPercentual);
    }

    [Fact]
    public async Task SimularAsync_ComTarifaNegativa_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(Pedido(tarifasContratacao: -1m)));
    }

    [Fact]
    public async Task SimularAsync_EntradaNaoEntraNoCalculoDoCet()
    {
        // O CET mede o custo do que o banco cobra, não o negócio do imóvel — a
        // entrada é capital próprio e nunca passa pelo credor.
        var semEntrada = await _sut.SimularAsync(Pedido(valorImovel: 500000m, taxaMensal: 0.8m));
        var comEntrada = await _sut.SimularAsync(Pedido(valorImovel: 500000m, entrada: 100000m, taxaMensal: 0.8m));

        Assert.Equal(semEntrada.Sac.CetMensalPercentual, comEntrada.Sac.CetMensalPercentual, 2);
    }

    // ---------- Comparação entre sistemas ----------

    [Fact]
    public async Task SimularAsync_ApontaOSacComoMaisBaratoEmJuros()
    {
        var resultado = await _sut.SimularAsync(Pedido());

        Assert.Equal(SistemaAmortizacao.Sac, resultado.SistemaMaisBarato);
        Assert.Equal(
            Math.Round(Math.Abs(resultado.Price.TotalJuros - resultado.Sac.TotalJuros), 2),
            resultado.DiferencaTotalJuros);
    }

    [Fact]
    public async Task SimularAsync_ComTaxaZero_OsDoisSistemasEmpatam()
    {
        var resultado = await _sut.SimularAsync(Pedido(taxaMensal: 0m));

        Assert.Equal(0m, resultado.DiferencaTotalJuros);
        Assert.Equal(SistemaAmortizacao.Price, resultado.SistemaMaisBarato);
    }
}
