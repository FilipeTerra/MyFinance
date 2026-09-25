using System;
using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Sugestao;

/// <summary>
/// O peso das compras parceladas sobre a capacidade de aporte: quanto ainda vence,
/// e quanto do orçamento volta a ficar livre conforme os parcelamentos terminam.
/// </summary>
/// <remarks>
/// As parcelas já estão dentro da despesa média do período — nada aqui é somado ao
/// diagnóstico. O que este bloco acrescenta é a data: a sobra livre de hoje não é a
/// de daqui a seis meses, e é isso que permite responder "espere" em vez de só "corte".
/// </remarks>
public class CompromissoDto
{
    /// <summary>Soma de todas as parcelas que ainda vão vencer.</summary>
    public decimal TotalComprometido { get; set; }

    /// <summary>Quanto de parcela vence no próximo mês — o peso atual sobre o orçamento.</summary>
    public decimal ParcelaDoProximoMes { get; set; }

    /// <summary>Quantidade de compras parceladas em aberto.</summary>
    public int QuantidadeDeCompras { get; set; }

    /// <summary>
    /// Mês em que o aporte passa a caber sem nenhum corte, só esperando os parcelamentos
    /// acabarem, no formato "aaaa-MM". Nulo quando já cabe hoje ou quando esperar não basta.
    /// </summary>
    public string? MesEmQueCabe { get; set; }

    /// <summary>Sobra livre projetada mês a mês, conforme os parcelamentos terminam.</summary>
    public IReadOnlyList<SobraProjetadaDto> Projecao { get; set; } = Array.Empty<SobraProjetadaDto>();
}

/// <summary>Sobra livre estimada para um mês futuro.</summary>
public class SobraProjetadaDto
{
    /// <summary>Mês no formato "aaaa-MM".</summary>
    public string Mes { get; set; } = string.Empty;

    /// <summary>Sobra livre estimada, já com os parcelamentos encerrados até então.</summary>
    public decimal SobraLivre { get; set; }

    /// <summary>Quanto do orçamento mensal terá sido liberado até este mês.</summary>
    public decimal ValorLiberado { get; set; }
}
