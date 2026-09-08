using MyFinance.Application.Services.StatementImport;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class StatementTextNormalizerTests
{
    [Theory]
    [InlineData("IFD*IFOOD CLUB", "IFD IFOOD CLUB")]
    [InlineData("IFD IFOOD CLUB         Osasco        BRA", "IFD IFOOD CLUB OSASCO")]
    [InlineData("Padaria São João", "PADARIA SAO JOAO")]
    [InlineData("CP PARC DUO GOURMET (Parcela 06 de 09)", "CP PARC DUO GOURMET")]
    [InlineData("JIM COM  58154130 GIO  BELO HORIZON  BRA", "JIM COM GIO BELO HORIZON")]
    [InlineData("", "")]
    public void Normalize_ProduzChaveCanonica(string entrada, string esperado)
    {
        Assert.Equal(esperado, StatementTextNormalizer.Normalize(entrada));
    }

    [Fact]
    public void MerchantToken_IgnoraPracaECodigoDoEstabelecimento()
    {
        // A mesma loja aparece com código diferente entre faturas; o token é o que
        // permite reconhecê-la mesmo assim.
        var comCodigo = StatementTextNormalizer.MerchantToken("JIM COM  58154130 GIO  BELO HORIZON  BRA");
        var semCodigo = StatementTextNormalizer.MerchantToken("JIM COM  LA GIO CONFE  BELO HORIZON  BRA");

        Assert.Equal(comCodigo, semCodigo);
        Assert.NotEqual(string.Empty, comCodigo);
    }

    [Fact]
    public void MerchantToken_DistingueComerciantesDiferentes()
    {
        Assert.NotEqual(
            StatementTextNormalizer.MerchantToken("POSTO TUNEL LTDA       BELO HORIZONT BRA"),
            StatementTextNormalizer.MerchantToken("POSTO COELHO LTDA      BELO HORIZONT BRA"));
    }

    [Fact]
    public void MerchantToken_DescartaDescricaoCurtaDemaisParaIdentificar()
    {
        Assert.Equal(string.Empty, StatementTextNormalizer.MerchantToken("A*"));
    }

    [Theory]
    [InlineData("DROGARIA", "Drogaria")]
    [InlineData("SUPERMERCADO", "Supermercado")]
    public void ToTitleCase_NormalizaNomeDeCategoria(string entrada, string esperado)
    {
        Assert.Equal(esperado, StatementTextNormalizer.ToTitleCase(entrada));
    }
}
