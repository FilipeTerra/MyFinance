using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class CetCalculatorTests
{
    /// <summary>Fluxo de um financiamento simples: recebe o principal, paga N parcelas iguais.</summary>
    private static List<decimal> FluxoSimples(decimal principal, decimal parcela, int n)
    {
        var fluxo = new List<decimal> { principal };
        fluxo.AddRange(Enumerable.Repeat(-parcela, n));
        return fluxo;
    }

    [Fact]
    public void Calcular_SemEncargosNemTarifas_CetMensalIgualaATaxaDoContrato()
    {
        // Validação cruzada mais forte que existe: um contrato sem nenhum
        // encargo tem, por definição, CET = taxa contratual. Uso o próprio
        // Price para gerar o fluxo, então a única incógnita é o solver.
        var resultado = FinanciamentoPriceCalculator.Calcular(50000m, 1.5m, 48);
        var fluxo = FluxoSimples(50000m, resultado.ValorParcela, 48);

        var cet = CetCalculator.Calcular(fluxo);

        Assert.True(cet.Convergiu);
        Assert.Equal(1.5m, cet.TaxaMensalPercentual, 2);
    }

    [Fact]
    public void Calcular_ComEncargos_CetEMaiorQueATaxaDoContrato()
    {
        var semEncargos = FinanciamentoPriceCalculator.Calcular(50000m, 1.5m, 48);
        var comEncargos = FinanciamentoPriceCalculator.Calcular(
            50000m, 1.5m, 48, null, new EncargosFinanciamento(0.03m, 0.01m, 50000m, 30m));

        var cetSemEncargos = CetCalculator.Calcular(FluxoSimples(50000m, semEncargos.ValorParcela, 48));
        var cetComEncargos = CetCalculator.Calcular(
            new[] { 50000m }.Concat(comEncargos.Parcelas.Select(p => -p.ParcelaTotal)).ToList());

        Assert.True(cetComEncargos.TaxaMensalPercentual > cetSemEncargos.TaxaMensalPercentual);
    }

    [Fact]
    public void Calcular_ComTarifaDeContratacao_AumentaOCet()
    {
        var resultado = FinanciamentoPriceCalculator.Calcular(50000m, 1.5m, 48);
        var semTarifa = CetCalculator.Calcular(FluxoSimples(50000m, resultado.ValorParcela, 48));
        // Tarifa reduz o líquido recebido sem mudar o que se paga — o custo
        // efetivo sobe, mesmo com a taxa de contrato intacta.
        var comTarifa = CetCalculator.Calcular(FluxoSimples(50000m - 1500m, resultado.ValorParcela, 48));

        Assert.True(comTarifa.TaxaMensalPercentual > semTarifa.TaxaMensalPercentual);
    }

    [Fact]
    public void Calcular_ComTaxaZeroSemEncargos_ConvergeParaZeroPorcento()
    {
        // Sem juros nem encargo algum, o total pago fecha exatamente com o
        // principal — a raiz é 0% mesmo, não um caso de não-convergência.
        var resultado = FinanciamentoPriceCalculator.Calcular(12000m, 0m, 12);

        var cet = CetCalculator.Calcular(FluxoSimples(12000m, resultado.ValorParcela, 12));

        Assert.True(cet.Convergiu);
        Assert.Equal(0m, cet.TaxaMensalPercentual);
        Assert.Equal(0m, cet.TaxaAnualPercentual);
    }

    [Fact]
    public void Calcular_ComTarifaMaiorOuIgualAoValorLiberado_NaoConverge()
    {
        // Nada foi de fato liberado ao tomador — não há sobre o que calcular uma TIR.
        var cet = CetCalculator.Calcular(FluxoSimples(0m, 100m, 12));

        Assert.False(cet.Convergiu);
        Assert.Equal(0m, cet.TaxaMensalPercentual);
    }

    [Fact]
    public void Calcular_ComFluxoDeUmaSoPosicao_NaoConverge()
    {
        var cet = CetCalculator.Calcular(new List<decimal> { 1000m });

        Assert.False(cet.Convergiu);
    }

    [Fact]
    public void Calcular_TaxaAnualEACapitalizacaoCompostaDaMensal()
    {
        var resultado = FinanciamentoPriceCalculator.Calcular(50000m, 1.5m, 48);

        var cet = CetCalculator.Calcular(FluxoSimples(50000m, resultado.ValorParcela, 48));

        var mensalFracao = cet.TaxaMensalPercentual / 100;
        var anualEsperada = System.Math.Round(
            ((decimal)System.Math.Pow((double)(1 + mensalFracao), 12) - 1) * 100, 1);
        Assert.Equal(anualEsperada, cet.TaxaAnualPercentual, 1);
    }
}
