using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Sugestao;

/// <summary>
/// Pede a sugestão de comportamento financeiro para uma meta reversa já calculada.
/// Carrega os mesmos parâmetros da projeção porque os cenários alternativos
/// (outro prazo, outro alvo) precisam simular de novo com a mesma taxa e o mesmo ativo.
/// </summary>
public record SugestaoAporteRequestDto
{
    public decimal AporteInicial { get; init; }
    public int PrazoMeses { get; init; }

    /// <summary>Valor líquido que se deseja atingir ao final do prazo.</summary>
    public decimal ValorAlvo { get; init; }

    /// <summary>
    /// Aporte mensal necessário, quando o cliente já o tem em tela. Serve só para evitar
    /// repetir a busca binária da meta reversa (dezenas de simulações) a cada vez que o
    /// usuário liga o toggle. Entra apenas em aritmética de orçamento — não autoriza nada
    /// nem é gravado. Quando omitido, o serviço recalcula.
    /// </summary>
    public decimal? AporteMensalNecessario { get; init; }

    public FonteTaxaJuros FonteTaxaJuros { get; init; }
    public decimal? TaxaJurosAnualPercentual { get; init; }
    public decimal? PercentualCdi { get; init; }
    public TipoAtivoCalculadora TipoAtivo { get; init; }
}
