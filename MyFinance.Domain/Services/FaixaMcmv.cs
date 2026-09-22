using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Condições de uma faixa do Minha Casa Minha Vida. Os valores são referência
    /// pública (ver <see cref="MinhaCasaMinhaVidaTabela.VigenciaReferencia"/>) —
    /// mudam por decreto e variam por região, então nunca são usados para calcular
    /// algo com mais precisão do que o programa de fato garante.
    /// </summary>
    /// <param name="RendaMaxima">Teto de renda familiar bruta mensal da faixa, em R$.</param>
    /// <param name="TaxaAnualMinimaPercentual">Piso da faixa de taxa de juros anual do programa.</param>
    /// <param name="TaxaAnualMaximaPercentual">
    /// Teto da faixa de taxa de juros anual — usado como taxa sugerida quando o
    /// usuário não informa a sua, por ser a leitura conservadora (nunca promete
    /// parcela menor do que a pior taxa possível na faixa).
    /// </param>
    /// <param name="TetoImovelReferencia">Valor de referência do teto de imóvel aceito pelo programa nessa faixa.</param>
    /// <param name="EntradaMinimaPercentual">Entrada mínima exigida, como % do imóvel.</param>
    /// <param name="SubsidioMaximoPercentualImovel">Só a Faixa 1: subsídio máximo como % do imóvel.</param>
    /// <param name="SubsidioMaximoValor">Só a Faixa 2: subsídio máximo em R$, valor fixo.</param>
    public record FaixaMcmv(
        FaixaMinhaCasaMinhaVida Faixa,
        decimal RendaMaxima,
        decimal TaxaAnualMinimaPercentual,
        decimal TaxaAnualMaximaPercentual,
        decimal TetoImovelReferencia,
        decimal EntradaMinimaPercentual,
        decimal? SubsidioMaximoPercentualImovel = null,
        decimal? SubsidioMaximoValor = null);
}
