using System;
using System.Collections.Generic;
using MyFinance.Domain.Enums;

namespace MyFinance.Application.Dtos.Sugestao;

/// <summary>
/// Diagnóstico de como o usuário precisaria se comportar financeiramente para
/// bancar o aporte da meta: se cabe no orçamento, quanto falta, de onde cortar e
/// que alternativas existem quando não cabe de jeito nenhum.
/// </summary>
public class SugestaoAporteResponseDto
{
    /// <summary>Aporte mensal exigido pela meta, que este diagnóstico avalia.</summary>
    public decimal AporteMensalNecessario { get; set; }

    /// <summary>O aporte cabe na sobra mensal, eventualmente após os cortes sugeridos.</summary>
    public bool Cabe { get; set; }

    public decimal RendaMensal { get; set; }
    public FonteRenda FonteRenda { get; set; }

    /// <summary>Média mensal de despesas no período analisado.</summary>
    public decimal DespesaMensalMedia { get; set; }

    /// <summary>Média mensal já destinada a aportes — sai da sobra, mas não é despesa.</summary>
    public decimal AportesMensaisMedios { get; set; }

    /// <summary>Renda menos despesas menos os aportes já em curso: o dinheiro de fato ocioso.</summary>
    public decimal SobraLivre { get; set; }

    /// <summary>Quanto falta por mês para bancar o aporte. Zero quando já cabe.</summary>
    public decimal Deficit { get; set; }

    /// <summary>Folga que ainda restaria depois do aporte. Zero quando não cabe.</summary>
    public decimal FolgaRestante { get; set; }

    /// <summary>Maior aporte mensal sustentável, já contando os cortes possíveis.</summary>
    public decimal CapacidadeMaxima { get; set; }

    /// <summary>Fatia da renda que o aporte consumiria (0 a 100).</summary>
    public decimal PercentualDaRenda { get; set; }

    /// <summary>Plano de corte sobre categorias discricionárias. Vazio quando o aporte já cabe.</summary>
    public IReadOnlyList<CorteSugeridoDto> Cortes { get; set; } = Array.Empty<CorteSugeridoDto>();

    /// <summary>
    /// Categorias que o usuário ainda não classificou, ordenadas por gasto. Nenhuma delas
    /// entra no plano de corte: a UI usa esta lista para pedir a classificação.
    /// </summary>
    public IReadOnlyList<CategoriaNaoClassificadaDto> NaoClassificadas { get; set; } = Array.Empty<CategoriaNaoClassificadaDto>();

    /// <summary>Diagnóstico da reserva de emergência. Nulo quando não há renda conhecida.</summary>
    public ReservaEmergenciaDto? Reserva { get; set; }

    /// <summary>Alternativas de prazo e de alvo. Preenchido apenas quando o aporte não cabe.</summary>
    public CenariosAlternativosDto? Cenarios { get; set; }

    /// <summary>Período efetivamente analisado.</summary>
    public DateTime InicioAnalise { get; set; }

    /// <summary>Fim do período analisado (último mês completo).</summary>
    public DateTime FimAnalise { get; set; }

    /// <summary>Meses com lançamentos encontrados no período.</summary>
    public int MesesAnalisados { get; set; }

    /// <summary>Menos de dois meses de dados: as médias são pouco confiáveis e a UI avisa.</summary>
    public bool HistoricoInsuficiente { get; set; }

    /// <summary>Texto consultivo em linguagem natural.</summary>
    public string TextoConsultivo { get; set; } = string.Empty;

    /// <summary>O texto consultivo foi redigido pelo agente de IA.</summary>
    public bool IaUsada { get; set; }

    /// <summary>
    /// O agente de IA não respondeu (ou respondeu de forma não confiável) e o texto veio
    /// do template determinístico. Não é erro: o diagnóstico continua completo.
    /// </summary>
    public bool IaIndisponivel { get; set; }
}

/// <summary>Quanto o plano sugere cortar de uma categoria.</summary>
public class CorteSugeridoDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Gasto médio mensal atual da categoria.</summary>
    public decimal GastoAtual { get; set; }

    /// <summary>Valor mensal a cortar.</summary>
    public decimal ValorCorte { get; set; }

    /// <summary>Quanto sobraria para gastar na categoria depois do corte.</summary>
    public decimal GastoDepoisDoCorte { get; set; }
}

/// <summary>Categoria sem classificação, com o gasto que ela representa.</summary>
public class CategoriaNaoClassificadaDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal GastoMensalMedio { get; set; }
    public ExpenseNature Nature { get; set; } = ExpenseNature.NaoClassificado;
}

/// <summary>Situação da reserva de emergência frente ao ideal de seis meses de renda.</summary>
public class ReservaEmergenciaDto
{
    public bool Adequada { get; set; }
    public decimal ValorIdeal { get; set; }
    public decimal ValorAtual { get; set; }
    public decimal ValorFaltante { get; set; }
    public decimal PercentualAtingido { get; set; }
    public decimal MesesCobertos { get; set; }
}

/// <summary>Saídas possíveis quando a meta não cabe no orçamento como foi pedida.</summary>
public class CenariosAlternativosDto
{
    /// <summary>Aporte mensal usado nos dois cenários: o máximo que o usuário sustenta.</summary>
    public decimal AporteSustentavel { get; set; }

    /// <summary>Prazo em meses para atingir o alvo original com o aporte sustentável.</summary>
    public int? PrazoMesesAlternativo { get; set; }

    /// <summary>O alvo original é inatingível com o aporte sustentável, mesmo em 50 anos.</summary>
    public bool AlvoInatingivelNoPrazoMaximo { get; set; }

    /// <summary>Valor líquido que o aporte sustentável alcança dentro do prazo original.</summary>
    public decimal? ValorAlvoAlternativo { get; set; }
}
