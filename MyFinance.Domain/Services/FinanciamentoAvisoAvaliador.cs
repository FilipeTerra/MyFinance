using System;
using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Avalia a simulação contra regras de bom senso (e, a partir da Fase 5, do
    /// MCMV) e devolve avisos — nunca lança. É um simulador, não esteira de
    /// crédito: a renda ser apertada não impede o usuário de ver o resultado.
    /// </summary>
    public static class FinanciamentoAvisoAvaliador
    {
        private const decimal LimiteComprometimentoRendaPercentual = 30m;

        /// <param name="rendaMensal">Renda familiar bruta mensal, em R$. Nulo ou zero pula a checagem.</param>
        /// <param name="parcelaMensalComEncargos">Desembolso mensal já com extras, seguros e tarifa.</param>
        /// <param name="sistema">Sistema de amortização ao qual essa parcela pertence, para nomear o aviso.</param>
        public static IReadOnlyList<AvisoFinanciamento> Avaliar(
            decimal? rendaMensal, decimal parcelaMensalComEncargos, SistemaAmortizacao sistema)
        {
            var avisos = new List<AvisoFinanciamento>();

            if (rendaMensal is > 0)
            {
                var comprometimento = Math.Round(parcelaMensalComEncargos / rendaMensal.Value * 100, 2);

                if (comprometimento > LimiteComprometimentoRendaPercentual)
                {
                    avisos.Add(new AvisoFinanciamento(
                        "IncomeCommitmentAboveLimit",
                        $"No {NomeSistema(sistema)}, a parcela compromete {comprometimento:0.00}% da renda " +
                        "informada — acima dos 30% que os bancos costumam aceitar na análise de crédito.",
                        SeveridadeAvisoFinanciamento.Atencao));
                }
            }

            return avisos;
        }

        /// <summary>
        /// Avisos específicos do Minha Casa Minha Vida. Toda violação de regra do
        /// programa vira aviso, nunca exceção — é um simulador, não esteira de
        /// crédito, e derrubar a simulação por causa de renda fora da faixa
        /// impediria o usuário de ver qualquer resultado.
        /// </summary>
        /// <param name="faixa">Faixa resolvida pela renda, ou nulo quando a renda está fora do programa.</param>
        /// <param name="rendaMensal">Renda usada para tentar resolver a faixa, só para compor a mensagem.</param>
        /// <param name="composicao">Composição já resolvida (imóvel, entrada, subsídio).</param>
        /// <param name="numParcelas">Prazo contratado, em meses.</param>
        public static IReadOnlyList<AvisoFinanciamento> AvaliarMcmv(
            FaixaMcmv? faixa, decimal rendaMensal, ComposicaoFinanciamento composicao, int numParcelas)
        {
            var avisos = new List<AvisoFinanciamento>();

            if (faixa is null)
            {
                avisos.Add(new AvisoFinanciamento(
                    "IncomeAboveMcmvProgramLimit",
                    $"A renda informada ({FormatarReais(rendaMensal)}) está acima do teto do Minha Casa Minha Vida " +
                    $"({FormatarReais(MinhaCasaMinhaVidaTabela.RendaMaximaDoPrograma)}) — o programa não se aplica " +
                    "com esses parâmetros; a simulação seguiu com a taxa e as condições que você informou.",
                    SeveridadeAvisoFinanciamento.Atencao));
            }
            else
            {
                var nomeFaixa = $"Faixa {(int)faixa.Faixa}";

                if (composicao.ValorImovel > faixa.TetoImovelReferencia)
                {
                    avisos.Add(new AvisoFinanciamento(
                        "PropertyPriceAboveReferenceLimit",
                        $"O valor do imóvel está acima do teto de referência da {nomeFaixa} " +
                        $"({FormatarReais(faixa.TetoImovelReferencia)}) — o teto real varia por região; confirme com a Caixa.",
                        SeveridadeAvisoFinanciamento.Atencao));
                }

                if (composicao.EntradaPercentual < faixa.EntradaMinimaPercentual)
                {
                    avisos.Add(new AvisoFinanciamento(
                        "DownPaymentBelowMinimum",
                        $"A entrada informada ({composicao.EntradaPercentual:0.0}%) está abaixo do mínimo de " +
                        $"{faixa.EntradaMinimaPercentual:0}% que a {nomeFaixa} costuma exigir.",
                        SeveridadeAvisoFinanciamento.Atencao));
                }

                if (composicao.Subsidio > 0)
                {
                    var subsidioMaximo = faixa.SubsidioMaximoPercentualImovel is { } percentual
                        ? composicao.ValorImovel * percentual / 100
                        : faixa.SubsidioMaximoValor ?? 0m;

                    if (composicao.Subsidio > subsidioMaximo)
                    {
                        avisos.Add(new AvisoFinanciamento(
                            "SubsidyAboveLimit",
                            $"O subsídio informado ({FormatarReais(composicao.Subsidio)}) está acima do teto de até " +
                            $"{FormatarReais(subsidioMaximo)} da {nomeFaixa}.",
                            SeveridadeAvisoFinanciamento.Atencao));
                    }
                }
            }

            if (numParcelas > 420)
            {
                avisos.Add(new AvisoFinanciamento(
                    "TermAboveMaximum",
                    $"O prazo informado ({numParcelas} meses) está acima do máximo de 420 meses (35 anos) do programa.",
                    SeveridadeAvisoFinanciamento.Atencao));
            }

            avisos.Add(new AvisoFinanciamento(
                "SacIsThePredominantSystem",
                "A Caixa usa predominantemente o SAC nos financiamentos do Minha Casa Minha Vida — o Price também é " +
                "simulado aqui para comparação, mas pode não estar disponível na prática.",
                SeveridadeAvisoFinanciamento.Informativo));

            return avisos;
        }

        private static string NomeSistema(SistemaAmortizacao sistema) =>
            sistema == SistemaAmortizacao.Price ? "sistema Price" : "SAC";

        private static string FormatarReais(decimal valor) =>
            valor.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    }
}
