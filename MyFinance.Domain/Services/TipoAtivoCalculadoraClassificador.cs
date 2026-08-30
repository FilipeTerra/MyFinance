using System;
using MyFinance.Domain.Enums;

namespace MyFinance.Domain.Services
{
    /// <summary>
    /// Resolve o regime de tributação (<see cref="CategoriaTributariaAtivo"/>) de um
    /// <see cref="TipoAtivoCalculadora"/> — o único lugar onde esse mapeamento existe.
    /// </summary>
    public static class TipoAtivoCalculadoraClassificador
    {
        public static CategoriaTributariaAtivo Classificar(TipoAtivoCalculadora tipo)
        {
            return tipo switch
            {
                TipoAtivoCalculadora.Cdb => CategoriaTributariaAtivo.RendaFixaTributavel,
                TipoAtivoCalculadora.Rdb => CategoriaTributariaAtivo.RendaFixaTributavel,
                TipoAtivoCalculadora.TesouroSelic => CategoriaTributariaAtivo.RendaFixaTributavel,
                TipoAtivoCalculadora.TesouroIpca => CategoriaTributariaAtivo.RendaFixaTributavel,
                TipoAtivoCalculadora.TesouroPrefixado => CategoriaTributariaAtivo.RendaFixaTributavel,
                TipoAtivoCalculadora.Lci => CategoriaTributariaAtivo.RendaFixaIsenta,
                TipoAtivoCalculadora.Lca => CategoriaTributariaAtivo.RendaFixaIsenta,
                TipoAtivoCalculadora.Acao => CategoriaTributariaAtivo.GanhoCapitalAcao,
                TipoAtivoCalculadora.Fii => CategoriaTributariaAtivo.GanhoCapitalFii,
                TipoAtivoCalculadora.Cripto => CategoriaTributariaAtivo.GanhoCapitalCripto,
                TipoAtivoCalculadora.FundoAcoes => CategoriaTributariaAtivo.GanhoCapitalFundoAcoes,
                TipoAtivoCalculadora.FundoRendaFixaLongoPrazo => CategoriaTributariaAtivo.FundoComeCotasLongoPrazo,
                TipoAtivoCalculadora.FundoMultimercado => CategoriaTributariaAtivo.FundoComeCotasLongoPrazo,
                TipoAtivoCalculadora.FundoRendaFixaCurtoPrazo => CategoriaTributariaAtivo.FundoComeCotasCurtoPrazo,
                TipoAtivoCalculadora.Pgbl => CategoriaTributariaAtivo.PrevidenciaPgbl,
                TipoAtivoCalculadora.Vgbl => CategoriaTributariaAtivo.PrevidenciaVgbl,
                _ => throw new ArgumentException("Tipo de ativo inválido ou não informado.", nameof(tipo))
            };
        }
    }
}
