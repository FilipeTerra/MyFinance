using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class FinanciamentoAvisoAvaliadorTests
{
    [Fact]
    public void Avaliar_SemRendaInformada_NaoGeraAviso()
    {
        var avisos = FinanciamentoAvisoAvaliador.Avaliar(null, 5000m, SistemaAmortizacao.Sac);

        Assert.Empty(avisos);
    }

    [Fact]
    public void Avaliar_ComRendaZero_NaoGeraAviso()
    {
        var avisos = FinanciamentoAvisoAvaliador.Avaliar(0m, 5000m, SistemaAmortizacao.Sac);

        Assert.Empty(avisos);
    }

    [Fact]
    public void Avaliar_ComParcelaAbaixoDoLimite_NaoGeraAviso()
    {
        // 2900 / 10000 = 29% — abaixo do teto de 30%.
        var avisos = FinanciamentoAvisoAvaliador.Avaliar(10000m, 2900m, SistemaAmortizacao.Sac);

        Assert.Empty(avisos);
    }

    [Fact]
    public void Avaliar_ComParcelaAcimaDoLimite_GeraAvisoComCodigoEstavel()
    {
        // 3500 / 10000 = 35% — acima do teto de 30%.
        var avisos = FinanciamentoAvisoAvaliador.Avaliar(10000m, 3500m, SistemaAmortizacao.Sac);

        var aviso = Assert.Single(avisos);
        Assert.Equal("IncomeCommitmentAboveLimit", aviso.Codigo);
        Assert.Equal(SeveridadeAvisoFinanciamento.Atencao, aviso.Severidade);
        Assert.Contains("35", aviso.Mensagem);
        Assert.Contains("SAC", aviso.Mensagem);
    }

    [Fact]
    public void Avaliar_ComParcelaExatamenteNoLimite_NaoGeraAviso()
    {
        // Exatamente 30% não é "acima" do limite.
        var avisos = FinanciamentoAvisoAvaliador.Avaliar(10000m, 3000m, SistemaAmortizacao.Price);

        Assert.Empty(avisos);
    }

    [Fact]
    public void Avaliar_NomeiaOSistemaCorretamenteNaMensagem()
    {
        var avisoPrice = Assert.Single(FinanciamentoAvisoAvaliador.Avaliar(10000m, 3500m, SistemaAmortizacao.Price));
        var avisoSac = Assert.Single(FinanciamentoAvisoAvaliador.Avaliar(10000m, 3500m, SistemaAmortizacao.Sac));

        Assert.Contains("Price", avisoPrice.Mensagem);
        Assert.Contains("SAC", avisoSac.Mensagem);
    }

    // ---------- MCMV ----------

    private static ComposicaoFinanciamento Composicao(
        decimal valorImovel = 200000m, decimal entrada = 40000m, decimal subsidio = 0m) =>
        ComposicaoFinanciamento.Resolver(valorImovel, entrada, null, subsidio: subsidio);

    [Fact]
    public void AvaliarMcmv_ComRendaAcimaDoTeto_GeraAvisoDeForaDoPrograma()
    {
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(null, 15000m, Composicao(), 360);

        Assert.Contains(avisos, a => a.Codigo == "IncomeAboveMcmvProgramLimit");
        Assert.Contains(avisos, a => a.Mensagem.Contains("15.000") && a.Mensagem.Contains("13.000"));
    }

    [Fact]
    public void AvaliarMcmv_SemFaixa_NaoChecaImovelEntradaOuSubsidio()
    {
        // Sem faixa resolvida não há teto/mínimo contra o que comparar.
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            null, 15000m, Composicao(valorImovel: 900000m, entrada: 0m), 360);

        Assert.DoesNotContain(avisos, a => a.Codigo is "PropertyPriceAboveReferenceLimit" or "DownPaymentBelowMinimum");
    }

    [Fact]
    public void AvaliarMcmv_ComImovelAcimaDoTeto_GeraAvisoComAPalavraReferencia()
    {
        var faixa1 = MinhaCasaMinhaVidaTabela.Faixas[0];
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            faixa1, 3000m, Composicao(valorImovel: 300000m, entrada: 30000m), 360);

        var aviso = Assert.Single(avisos, a => a.Codigo == "PropertyPriceAboveReferenceLimit");
        Assert.Contains("referência", aviso.Mensagem);
    }

    [Fact]
    public void AvaliarMcmv_ComImovelDentroDoTeto_NaoGeraAviso()
    {
        var faixa1 = MinhaCasaMinhaVidaTabela.Faixas[0];
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            faixa1, 3000m, Composicao(valorImovel: 200000m, entrada: 30000m), 360);

        Assert.DoesNotContain(avisos, a => a.Codigo == "PropertyPriceAboveReferenceLimit");
    }

    [Fact]
    public void AvaliarMcmv_ComEntradaAbaixoDoMinimo_GeraAviso()
    {
        var faixa1 = MinhaCasaMinhaVidaTabela.Faixas[0]; // entrada mínima 10%
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            faixa1, 3000m, Composicao(valorImovel: 200000m, entrada: 5000m), 360); // 2,5%

        Assert.Contains(avisos, a => a.Codigo == "DownPaymentBelowMinimum");
    }

    [Fact]
    public void AvaliarMcmv_ComEntradaExatamenteNoMinimo_NaoGeraAviso()
    {
        var faixa1 = MinhaCasaMinhaVidaTabela.Faixas[0]; // entrada mínima 10%
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            faixa1, 3000m, Composicao(valorImovel: 200000m, entrada: 20000m), 360); // exatamente 10%

        Assert.DoesNotContain(avisos, a => a.Codigo == "DownPaymentBelowMinimum");
    }

    [Fact]
    public void AvaliarMcmv_ComSubsidioAcimaDoTetoPercentualDaFaixa1_GeraAviso()
    {
        var faixa1 = MinhaCasaMinhaVidaTabela.Faixas[0]; // até 95% do imóvel
        // Imóvel 200k, entrada 5k, subsídio 190k -> 95% seria 190k exato; 190.001 estoura.
        var composicao = ComposicaoFinanciamento.Resolver(200000m, 5000m, null, subsidio: 190001m);
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(faixa1, 3000m, composicao, 360);

        Assert.Contains(avisos, a => a.Codigo == "SubsidyAboveLimit");
    }

    [Fact]
    public void AvaliarMcmv_ComSubsidioDentroDoTetoDaFaixa1_NaoGeraAviso()
    {
        var faixa1 = MinhaCasaMinhaVidaTabela.Faixas[0];
        var composicao = ComposicaoFinanciamento.Resolver(200000m, 5000m, null, subsidio: 190000m);
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(faixa1, 3000m, composicao, 360);

        Assert.DoesNotContain(avisos, a => a.Codigo == "SubsidyAboveLimit");
    }

    [Fact]
    public void AvaliarMcmv_ComSubsidioAcimaDoTetoFixoDaFaixa2_GeraAviso()
    {
        var faixa2 = MinhaCasaMinhaVidaTabela.Faixas[1]; // até R$55.000
        var composicao = ComposicaoFinanciamento.Resolver(200000m, 20000m, null, subsidio: 55001m);
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(faixa2, 4000m, composicao, 360);

        var aviso = Assert.Single(avisos, a => a.Codigo == "SubsidyAboveLimit");
        Assert.Contains("55.000", aviso.Mensagem);
    }

    [Fact]
    public void AvaliarMcmv_ComQualquerSubsidioNaFaixa3_GeraAviso()
    {
        var faixa3 = MinhaCasaMinhaVidaTabela.Faixas[2]; // sem subsídio
        var composicao = ComposicaoFinanciamento.Resolver(300000m, 60000m, null, subsidio: 1000m);
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(faixa3, 7000m, composicao, 360);

        Assert.Contains(avisos, a => a.Codigo == "SubsidyAboveLimit");
    }

    [Fact]
    public void AvaliarMcmv_ComPrazoAcimaDoMaximo_GeraAviso()
    {
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            MinhaCasaMinhaVidaTabela.Faixas[0], 3000m, Composicao(), 421);

        Assert.Contains(avisos, a => a.Codigo == "TermAboveMaximum");
    }

    [Fact]
    public void AvaliarMcmv_ComPrazoNoMaximo_NaoGeraAviso()
    {
        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            MinhaCasaMinhaVidaTabela.Faixas[0], 3000m, Composicao(), 420);

        Assert.DoesNotContain(avisos, a => a.Codigo == "TermAboveMaximum");
    }

    [Fact]
    public void AvaliarMcmv_SempreIncluiOAvisoInformativoDoSac()
    {
        var comFaixa = FinanciamentoAvisoAvaliador.AvaliarMcmv(MinhaCasaMinhaVidaTabela.Faixas[0], 3000m, Composicao(), 360);
        var semFaixa = FinanciamentoAvisoAvaliador.AvaliarMcmv(null, 15000m, Composicao(), 360);

        Assert.Contains(comFaixa, a => a.Codigo == "SacIsThePredominantSystem" && a.Severidade == SeveridadeAvisoFinanciamento.Informativo);
        Assert.Contains(semFaixa, a => a.Codigo == "SacIsThePredominantSystem");
    }

    [Fact]
    public void AvaliarMcmv_NuncaLancaMesmoComTodasAsRegrasViolads()
    {
        var composicao = ComposicaoFinanciamento.Resolver(900000m, 0m, null, subsidio: 0m);

        var avisos = FinanciamentoAvisoAvaliador.AvaliarMcmv(
            MinhaCasaMinhaVidaTabela.Faixas[0], 3000m, composicao, 500);

        Assert.NotEmpty(avisos);
    }
}
