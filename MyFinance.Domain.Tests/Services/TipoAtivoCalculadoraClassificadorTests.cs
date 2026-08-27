using MyFinance.Domain.Enums;
using MyFinance.Domain.Services;

namespace MyFinance.Domain.Tests.Services;

public class TipoAtivoCalculadoraClassificadorTests
{
    [Theory]
    [InlineData(TipoAtivoCalculadora.Cdb, CategoriaTributariaAtivo.RendaFixaTributavel)]
    [InlineData(TipoAtivoCalculadora.Rdb, CategoriaTributariaAtivo.RendaFixaTributavel)]
    [InlineData(TipoAtivoCalculadora.TesouroSelic, CategoriaTributariaAtivo.RendaFixaTributavel)]
    [InlineData(TipoAtivoCalculadora.TesouroIpca, CategoriaTributariaAtivo.RendaFixaTributavel)]
    [InlineData(TipoAtivoCalculadora.TesouroPrefixado, CategoriaTributariaAtivo.RendaFixaTributavel)]
    [InlineData(TipoAtivoCalculadora.Lci, CategoriaTributariaAtivo.RendaFixaIsenta)]
    [InlineData(TipoAtivoCalculadora.Lca, CategoriaTributariaAtivo.RendaFixaIsenta)]
    [InlineData(TipoAtivoCalculadora.Acao, CategoriaTributariaAtivo.GanhoCapitalAcao)]
    [InlineData(TipoAtivoCalculadora.Fii, CategoriaTributariaAtivo.GanhoCapitalFii)]
    [InlineData(TipoAtivoCalculadora.Cripto, CategoriaTributariaAtivo.GanhoCapitalCripto)]
    [InlineData(TipoAtivoCalculadora.FundoAcoes, CategoriaTributariaAtivo.GanhoCapitalFundoAcoes)]
    [InlineData(TipoAtivoCalculadora.FundoRendaFixaLongoPrazo, CategoriaTributariaAtivo.FundoComeCotasLongoPrazo)]
    [InlineData(TipoAtivoCalculadora.FundoMultimercado, CategoriaTributariaAtivo.FundoComeCotasLongoPrazo)]
    [InlineData(TipoAtivoCalculadora.FundoRendaFixaCurtoPrazo, CategoriaTributariaAtivo.FundoComeCotasCurtoPrazo)]
    [InlineData(TipoAtivoCalculadora.Pgbl, CategoriaTributariaAtivo.PrevidenciaPgbl)]
    [InlineData(TipoAtivoCalculadora.Vgbl, CategoriaTributariaAtivo.PrevidenciaVgbl)]
    public void Classificar_ReturnsExpectedCategoria(TipoAtivoCalculadora tipo, CategoriaTributariaAtivo esperado)
    {
        Assert.Equal(esperado, TipoAtivoCalculadoraClassificador.Classificar(tipo));
    }

    [Fact]
    public void Classificar_WithInvalidValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TipoAtivoCalculadoraClassificador.Classificar((TipoAtivoCalculadora)0));
        Assert.Throws<ArgumentException>(() => TipoAtivoCalculadoraClassificador.Classificar((TipoAtivoCalculadora)99));
    }
}
