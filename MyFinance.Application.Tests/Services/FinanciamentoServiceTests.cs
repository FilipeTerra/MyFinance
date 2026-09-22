using Moq;
using MyFinance.Application.Dtos.Financiamento;
using MyFinance.Application.Dtos.Investimentos;
using MyFinance.Application.Interfaces.Services;
using MyFinance.Application.Services;
using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Application.Tests.Services;

public class FinanciamentoServiceTests
{
    private readonly Mock<IProjecaoInvestimentoService> _projecaoService = new();
    private readonly FinanciamentoService _sut;

    public FinanciamentoServiceTests()
    {
        _sut = new FinanciamentoService(_projecaoService.Object);
    }

    private static FinanciamentoRequestDto Pedido(
        decimal? valorImovel = null,
        decimal? entrada = null,
        decimal? entradaPercentual = null,
        decimal valorFinanciado = 200000m,
        decimal? taxaMensal = 0.8m,
        int numParcelas = 240,
        decimal extraMensal = 0m,
        List<AmortizacaoExtraAvulsaDto>? avulsas = null,
        ModoAmortizacaoExtra modo = ModoAmortizacaoExtra.ReduzirPrazo,
        decimal mip = 0m,
        decimal dfi = 0m,
        decimal taxaAdministracao = 0m,
        decimal tarifasContratacao = 0m,
        decimal itbi = 0m,
        decimal custosCartorio = 0m,
        decimal? rendaMensal = null,
        bool minhaCasaMinhaVida = false,
        decimal? subsidioInformado = null) =>
        new()
        {
            SeguroMipMensalPercentualSaldo = mip,
            SeguroDfiMensalPercentualImovel = dfi,
            TaxaAdministracaoMensal = taxaAdministracao,
            TarifasContratacao = tarifasContratacao,
            Itbi = itbi,
            CustosCartorio = custosCartorio,
            RendaMensal = rendaMensal,
            MinhaCasaMinhaVida = minhaCasaMinhaVida,
            SubsidioInformado = subsidioInformado,
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

    // ---------- Custo de aquisição ----------

    [Fact]
    public async Task SimularAsync_SemItbiNemCartorio_DesembolsoInicialIgualaAEntrada()
    {
        var resultado = await _sut.SimularAsync(Pedido(valorImovel: 500000m, entrada: 100000m));

        Assert.Equal(0m, resultado.Composicao.Itbi);
        Assert.Equal(0m, resultado.Composicao.CustosCartorio);
        Assert.Equal(100000m, resultado.Composicao.DesembolsoInicial);
    }

    [Fact]
    public async Task SimularAsync_ComItbiECartorio_SomamNoDesembolsoInicialSemAlterarOFinanciado()
    {
        var resultado = await _sut.SimularAsync(Pedido(
            valorImovel: 500000m, entrada: 100000m, itbi: 15000m, custosCartorio: 4000m));

        Assert.Equal(15000m, resultado.Composicao.Itbi);
        Assert.Equal(4000m, resultado.Composicao.CustosCartorio);
        Assert.Equal(119000m, resultado.Composicao.DesembolsoInicial);
        Assert.Equal(400000m, resultado.Composicao.ValorFinanciado);
    }

    [Fact]
    public async Task SimularAsync_ComItbiNegativo_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(Pedido(itbi: -1m)));
    }

    // ---------- Comprometimento de renda e avisos ----------

    [Fact]
    public async Task SimularAsync_SemRenda_NaoGeraAvisoENaoCalculaComprometimento()
    {
        var resultado = await _sut.SimularAsync(Pedido());

        Assert.Empty(resultado.Avisos);
        Assert.Equal(0m, resultado.Sac.ComprometimentoRendaPercentual);
    }

    [Fact]
    public async Task SimularAsync_ComRendaFolgada_NaoGeraAviso()
    {
        var resultado = await _sut.SimularAsync(Pedido(rendaMensal: 50000m));

        Assert.Empty(resultado.Avisos);
        Assert.True(resultado.Sac.ComprometimentoRendaPercentual > 0);
    }

    [Fact]
    public async Task SimularAsync_ComRendaApertada_GeraAvisoParaOSistemaQueEstoura()
    {
        // Parcelas de um financiamento de 200k/240x pesam bem mais que 30% de 1500.
        var resultado = await _sut.SimularAsync(Pedido(rendaMensal: 1500m));

        Assert.NotEmpty(resultado.Avisos);
        Assert.Contains(resultado.Avisos, a => a.Codigo == "IncomeCommitmentAboveLimit");
        Assert.All(resultado.Avisos, a => Assert.Equal("Atencao", a.Severidade));
    }

    [Fact]
    public async Task SimularAsync_ComRendaApertada_NaoLancaExcecao()
    {
        // É um simulador, não esteira de crédito — renda apertada nunca derruba a simulação.
        var resultado = await _sut.SimularAsync(Pedido(rendaMensal: 100m));

        Assert.True(resultado.Sac.ComprometimentoRendaPercentual > 30m);
    }

    // ---------- MCMV ----------

    [Fact]
    public async Task SimularAsync_ComMcmvSemValorDoImovel_ThrowsArgumentException()
    {
        var pedido = Pedido(minhaCasaMinhaVida: true, valorFinanciado: 200000m, rendaMensal: 3000m) with
        {
            ValorImovel = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(pedido));
    }

    [Fact]
    public async Task SimularAsync_ComMcmvSemRenda_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(
            Pedido(minhaCasaMinhaVida: true, valorImovel: 200000m, entrada: 20000m, rendaMensal: null)));
    }

    [Fact]
    public async Task SimularAsync_ComMcmvETaxaEmBranco_UsaOTetoDaFaixaResolvida()
    {
        // Renda 3000 -> Faixa 1, teto de 5,25% a.a.
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 200000m, entrada: 20000m, rendaMensal: 3000m, taxaMensal: null));

        Assert.NotNull(resultado.FaixaMcmv);
        Assert.Equal("Faixa1", resultado.FaixaMcmv!.Faixa);
        Assert.True(resultado.Sac.TotalJuros > 0);
    }

    [Fact]
    public async Task SimularAsync_SemMcmvETaxaEmBranco_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(Pedido(taxaMensal: null)));
    }

    [Fact]
    public async Task SimularAsync_ComMcmvETaxaInformada_NaoSobrescreveATaxaDoUsuario()
    {
        // Mesmo dentro do teto da Faixa 1, a taxa do usuário sempre vence.
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 200000m, entrada: 20000m, rendaMensal: 3000m, taxaMensal: 0.3m));

        var semMcmv = await _sut.SimularAsync(Pedido(valorImovel: 200000m, entrada: 20000m, taxaMensal: 0.3m));

        Assert.Equal(semMcmv.Sac.TotalJuros, resultado.Sac.TotalJuros);
    }

    [Fact]
    public async Task SimularAsync_ComRendaAcimaDoTetoDoPrograma_NaoResolveFaixaMasSimulaComATaxaInformada()
    {
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 900000m, entrada: 200000m, rendaMensal: 20000m, taxaMensal: 0.8m));

        Assert.Null(resultado.FaixaMcmv);
        Assert.True(resultado.Sac.TotalJuros > 0);
        Assert.Contains(resultado.Avisos, a => a.Codigo == "IncomeAboveMcmvProgramLimit");
    }

    [Fact]
    public async Task SimularAsync_ComRendaAcimaDoTetoETaxaEmBranco_ThrowsArgumentException()
    {
        // Sem faixa resolvida não há teto de taxa para usar como padrão.
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 900000m, entrada: 200000m, rendaMensal: 20000m, taxaMensal: null)));
    }

    [Fact]
    public async Task SimularAsync_ComSubsidio_ReduzOFinanciadoEApareceNaComposicao()
    {
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 200000m, entrada: 5000m, rendaMensal: 3000m,
            subsidioInformado: 100000m));

        Assert.Equal(100000m, resultado.Composicao.Subsidio);
        Assert.Equal(95000m, resultado.Composicao.ValorFinanciado);
    }

    [Fact]
    public async Task SimularAsync_SemMinhaCasaMinhaVida_IgnoraSubsidioInformado()
    {
        var resultado = await _sut.SimularAsync(Pedido(
            valorImovel: 200000m, entrada: 20000m, subsidioInformado: 50000m));

        Assert.Equal(0m, resultado.Composicao.Subsidio);
        Assert.Equal(180000m, resultado.Composicao.ValorFinanciado);
    }

    [Fact]
    public async Task SimularAsync_ComMcmv_IncluiAvisosDoProgramaNaLista()
    {
        // Imóvel acima do teto de referência da Faixa 1 (R$264.000).
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 300000m, entrada: 30000m, rendaMensal: 3000m, taxaMensal: 0.4m));

        Assert.Contains(resultado.Avisos, a => a.Codigo == "PropertyPriceAboveReferenceLimit");
        Assert.Contains(resultado.Avisos, a => a.Codigo == "SacIsThePredominantSystem");
    }

    [Fact]
    public async Task SimularAsync_SemMcmv_NaoIncluiAvisosDoPrograma()
    {
        var resultado = await _sut.SimularAsync(Pedido(valorImovel: 900000m, entrada: 30000m));

        Assert.DoesNotContain(resultado.Avisos, a =>
            a.Codigo is "PropertyPriceAboveReferenceLimit" or "SacIsThePredominantSystem"
                or "DownPaymentBelowMinimum" or "TermAboveMaximum" or "SubsidyAboveLimit" or "IncomeAboveMcmvProgramLimit");
        Assert.Null(resultado.FaixaMcmv);
        Assert.Null(resultado.VigenciaReferenciaMcmv);
    }

    [Fact]
    public async Task SimularAsync_ComMcmv_ExpoeAVigenciaDeReferencia()
    {
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 200000m, entrada: 20000m, rendaMensal: 3000m));

        Assert.Equal("2026-01", resultado.VigenciaReferenciaMcmv);
    }

    [Fact]
    public async Task SimularAsync_ComMcmvEViolacoesDoPrograma_NaoLanca()
    {
        // Entrada abaixo do mínimo, prazo acima do máximo, imóvel acima do teto —
        // nada disso pode derrubar a simulação.
        var resultado = await _sut.SimularAsync(Pedido(
            minhaCasaMinhaVida: true, valorImovel: 900000m, entrada: 9000m, rendaMensal: 3000m,
            taxaMensal: 0.4m, numParcelas: 421));

        Assert.True(resultado.Sac.TotalJuros > 0);
        Assert.NotEmpty(resultado.Avisos);
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

    // ---------- Amortizar vs investir ----------

    private static AmortizarVsInvestirRequestDto PedidoAmortizarVsInvestir(
        decimal valorFinanciado = 200000m,
        decimal taxaMensal = 0.8m,
        int numParcelas = 240,
        SistemaAmortizacao sistema = SistemaAmortizacao.Sac,
        decimal valorDisponivelMensal = 1000m,
        decimal? taxaInvestimentoAnual = 12m,
        TipoAtivoCalculadora tipoAtivo = TipoAtivoCalculadora.TesouroSelic) => new()
    {
        ValorFinanciado = valorFinanciado,
        TaxaJurosMensalPercentual = taxaMensal,
        NumParcelas = numParcelas,
        Sistema = sistema,
        ValorDisponivelMensal = valorDisponivelMensal,
        FonteTaxaJurosInvestimento = FonteTaxaJuros.Manual,
        TaxaJurosAnualInvestimentoPercentual = taxaInvestimentoAnual,
        TipoAtivoInvestimento = tipoAtivo
    };

    private void ConfigurarProjecao(decimal valorFinalLiquido) =>
        _projecaoService
            .Setup(p => p.CalcularProjecaoAsync(It.IsAny<CalcularProjecaoRequestDto>()))
            .ReturnsAsync(new ProjecaoInvestimentoResponseDto { ValorFinalLiquido = valorFinalLiquido });

    [Fact]
    public async Task AmortizarVsInvestirAsync_ChamaAProjecaoUmaUnicaVez()
    {
        ConfigurarProjecao(50000m);

        await _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir());

        _projecaoService.Verify(p => p.CalcularProjecaoAsync(It.IsAny<CalcularProjecaoRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_QuandoInvestirRendeMaisQueAEconomiaDeJuros_RecomendaInvestir()
    {
        ConfigurarProjecao(1_000_000m);

        var resultado = await _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir());

        Assert.Equal(RecomendacaoFinanceira.Investir, resultado.Recomendacao);
        Assert.True(resultado.Diferenca > 0);
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_QuandoInvestirRendeMenosQueAEconomiaDeJuros_RecomendaAmortizar()
    {
        ConfigurarProjecao(1m);

        var resultado = await _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir());

        Assert.Equal(RecomendacaoFinanceira.Amortizar, resultado.Recomendacao);
        Assert.True(resultado.Diferenca < 0);
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_PassaOValorDisponivelComoAporteMensalNaProjecao()
    {
        CalcularProjecaoRequestDto? capturado = null;
        _projecaoService
            .Setup(p => p.CalcularProjecaoAsync(It.IsAny<CalcularProjecaoRequestDto>()))
            .Callback<CalcularProjecaoRequestDto>(r => capturado = r)
            .ReturnsAsync(new ProjecaoInvestimentoResponseDto { ValorFinalLiquido = 0m });

        await _sut.AmortizarVsInvestirAsync(
            PedidoAmortizarVsInvestir(valorDisponivelMensal: 1500m, numParcelas: 180));

        Assert.NotNull(capturado);
        Assert.Equal(1500m, capturado!.AporteMensal);
        Assert.Equal(0m, capturado.AporteInicial);
        Assert.Equal(180, capturado.PrazoMeses);
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_EconomiaDeJurosBateComADiferencaEntreComESemExtra()
    {
        ConfigurarProjecao(0m);

        var semExtra = FinanciamentoSacCalculator.Calcular(200000m, 0.8m, 240);
        var comExtra = FinanciamentoSacCalculator.Calcular(
            200000m, 0.8m, 240, new AmortizacaoExtra(1000m, null, ModoAmortizacaoExtra.ReduzirPrazo));
        var economiaEsperada = Math.Round(semExtra.TotalJuros - comExtra.TotalJuros, 2);

        var resultado = await _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir());

        Assert.Equal(economiaEsperada, resultado.EconomiaJurosAmortizando);
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_ComSistemaPrice_UsaOCalculadorPrice()
    {
        ConfigurarProjecao(0m);

        var resultado = await _sut.AmortizarVsInvestirAsync(
            PedidoAmortizarVsInvestir(sistema: SistemaAmortizacao.Price));

        var semExtra = FinanciamentoPriceCalculator.Calcular(200000m, 0.8m, 240);
        var comExtra = FinanciamentoPriceCalculator.Calcular(
            200000m, 0.8m, 240, new AmortizacaoExtra(1000m, null, ModoAmortizacaoExtra.ReduzirPrazo));
        var economiaEsperada = Math.Round(semExtra.TotalJuros - comExtra.TotalJuros, 2);

        Assert.Equal(economiaEsperada, resultado.EconomiaJurosAmortizando);
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_ComValorDisponivelZero_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir(valorDisponivelMensal: 0m)));
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_ComValorDisponivelNegativo_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir(valorDisponivelMensal: -100m)));
    }

    [Fact]
    public async Task AmortizarVsInvestirAsync_DevolveAProjecaoCompletaParaODetalhamento()
    {
        var projecaoEsperada = new ProjecaoInvestimentoResponseDto { ValorFinalLiquido = 123m, TotalJuros = 45m };
        _projecaoService
            .Setup(p => p.CalcularProjecaoAsync(It.IsAny<CalcularProjecaoRequestDto>()))
            .ReturnsAsync(projecaoEsperada);

        var resultado = await _sut.AmortizarVsInvestirAsync(PedidoAmortizarVsInvestir());

        Assert.Same(projecaoEsperada, resultado.ProjecaoInvestindo);
        Assert.Equal(123m, resultado.ValorFinalLiquidoInvestindo);
    }
}
