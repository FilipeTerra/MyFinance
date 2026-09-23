using System;
using System.Collections.Generic;

namespace MyFinance.Application.Dtos.Sugestao;

/// <summary>
/// Bloco de fatos já calculados enviado ao agente de IA para que ele redija o texto
/// consultivo. O agente recebe números prontos e só os parafraseia — nada aqui é
/// recalculado do outro lado, e nenhum valor novo pode aparecer na resposta.
/// </summary>
public record SuggestionFactsDto
{
    public decimal AporteMensalNecessario { get; init; }
    public bool Cabe { get; init; }
    public decimal RendaMensal { get; init; }
    public decimal DespesaMensalMedia { get; init; }
    public decimal SobraLivre { get; init; }
    public decimal Deficit { get; init; }
    public decimal FolgaRestante { get; init; }
    public decimal PercentualDaRenda { get; init; }

    /// <summary>Cortes sugeridos, como pares (categoria, valor mensal a cortar).</summary>
    public IReadOnlyList<CorteFatoDto> Cortes { get; init; } = Array.Empty<CorteFatoDto>();

    /// <summary>Quanto falta para a reserva de emergência, quando ela não está completa.</summary>
    public decimal? ReservaFaltante { get; init; }

    /// <summary>Prazo alternativo em meses, quando a meta não cabe no prazo pedido.</summary>
    public int? PrazoMesesAlternativo { get; init; }

    /// <summary>Alvo alcançável no prazo original, quando a meta não cabe.</summary>
    public decimal? ValorAlvoAlternativo { get; init; }
}

/// <summary>Um corte sugerido, reduzido ao que o texto precisa citar.</summary>
public record CorteFatoDto(string Categoria, decimal ValorCorte);
