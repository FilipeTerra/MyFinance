using MyFinance.Application.Dtos.StatementImport;
using MyFinance.Application.Services.StatementImport;
using MyFinance.Domain.Entities;

namespace MyFinance.Application.Tests.Services.StatementImport;

public class CategoryResolverTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Category _transporte;
    private readonly Category _supermercado;

    public CategoryResolverTests()
    {
        _transporte = new Category("Transporte", _userId);
        _supermercado = new Category("Supermercado", _userId);
    }

    [Fact]
    public void Resolve_RegraAprendidaVenceOHistorico()
    {
        var context = Build(
            rules: new[] { Rule("DL UBERRIDES SAO PAULO", _transporte.Id) },
            history: new[] { new DescriptionCategoryCount("DL*UberRides Sao Paulo BRA", _supermercado.Id, 9) });

        var resolved = Resolve("DL*UberRides Sao Paulo BRA");

        Assert.Equal(_transporte.Id, resolved(context).CategoryId);
    }

    [Fact]
    public void Resolve_HistoricoComMesmaDescricaoAtribuiCategoria()
    {
        var context = Build(
            history: new[] { new DescriptionCategoryCount("CARREFOUR BHP 51 BELO HORIZONT BRA", _supermercado.Id, 4) });

        var dto = Resolve("CARREFOUR BHP 51       BELO HORIZONT BRA")(context);

        Assert.Equal(_supermercado.Id, dto.CategoryId);
        Assert.False(dto.IsSuggestion);
    }

    [Fact]
    public void Resolve_HistoricoUsaACategoriaMaisFrequente()
    {
        var context = Build(history: new[]
        {
            new DescriptionCategoryCount("PADARIA CENTRAL", _supermercado.Id, 7),
            new DescriptionCategoryCount("PADARIA CENTRAL", _transporte.Id, 2)
        });

        Assert.Equal(_supermercado.Id, Resolve("PADARIA CENTRAL")(context).CategoryId);
    }

    [Fact]
    public void Resolve_MesmoComercianteEmOutraPracaViraSugestao()
    {
        var context = Build(
            history: new[] { new DescriptionCategoryCount("JIM COM LA GIO CONFE BELO HORIZON BRA", _supermercado.Id, 3) });

        // Mesma loja, código diferente: dá para sugerir, mas não para afirmar.
        var dto = Resolve("JIM COM  58154130 GIO  BELO HORIZON  BRA")(context);

        Assert.Null(dto.CategoryId);
        Assert.True(dto.IsSuggestion);
        Assert.Equal("Supermercado", dto.SuggestedCategoryName);
    }

    [Fact]
    public void Resolve_CategoriaDoArquivoQueJaExisteEhAtribuida()
    {
        var dto = Resolve("LOJA NOVA", fileCategory: "Transporte")(Build());

        Assert.Equal(_transporte.Id, dto.CategoryId);
        Assert.False(dto.IsSuggestion);
    }

    [Fact]
    public void Resolve_CategoriaDoArquivoDesconhecidaViraSugestao()
    {
        var dto = Resolve("LOJA NOVA", fileCategory: "Viagem")(Build());

        Assert.Null(dto.CategoryId);
        Assert.True(dto.IsSuggestion);
        Assert.Equal("Viagem", dto.SuggestedCategoryName);
    }

    [Fact]
    public void Resolve_SemPistaDeixaEmBrancoParaOUsuario()
    {
        var dto = Resolve("ESTABELECIMENTO INEDITO")(Build());

        Assert.Null(dto.CategoryId);
        Assert.Null(dto.SuggestedCategoryName);
        Assert.False(dto.IsSuggestion);
        Assert.Single(CategoryResolver.Unresolved(new[] { dto }));
    }

    [Fact]
    public void Resolve_IgnoraRegraQueAponteParaCategoriaExcluida()
    {
        var context = Build(rules: new[] { Rule("LOJA ANTIGA", Guid.NewGuid()) });

        Assert.Null(Resolve("LOJA ANTIGA")(context).CategoryId);
    }

    [Fact]
    public void Resolve_PreservaDataValorEConta()
    {
        var entry = new ParsedStatementEntry(new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc), "IFOOD", -5.95m);

        var dto = Assert.Single(CategoryResolver.Resolve(new[] { entry }, _accountId, Build()));

        Assert.Equal(entry.Date, dto.Date);
        Assert.Equal(-5.95m, dto.Amount);
        Assert.Equal(_accountId, dto.AccountId);
    }

    private CategoryResolutionContext Build(
        IEnumerable<CategoryRule>? rules = null,
        IEnumerable<DescriptionCategoryCount>? history = null)
    {
        return CategoryResolutionContext.Build(
            new[] { _transporte, _supermercado },
            rules ?? Array.Empty<CategoryRule>(),
            history ?? Array.Empty<DescriptionCategoryCount>());
    }

    private CategoryRule Rule(string descriptionKey, Guid categoryId) =>
        new(_userId, descriptionKey, categoryId);

    private Func<CategoryResolutionContext, Dtos.AiTransactionResponseDto> Resolve(
        string description, string? fileCategory = null)
    {
        var entry = new ParsedStatementEntry(
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), description, -10m, fileCategory);

        return context => CategoryResolver.Resolve(new[] { entry }, _accountId, context).Single();
    }
}
