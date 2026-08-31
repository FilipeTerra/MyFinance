import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';

export type GrupoTipoAtivo = 'renda-fixa-tributavel' | 'renda-fixa-isenta' | 'renda-variavel' | 'fundos' | 'previdencia';

export interface TipoAtivoCalculadoraMeta {
    label: string;
    grupo: GrupoTipoAtivo;
}

/**
 * Metadados de apresentação para cada subtipo de ativo simulável na
 * calculadora. Centralizado para manter o seletor e o detalhamento de
 * tributos consistentes, sem rótulos soltos no componente.
 */
export const TIPO_ATIVO_CALCULADORA_META: Record<TipoAtivoCalculadora, TipoAtivoCalculadoraMeta> = {
    [TipoAtivoCalculadora.Cdb]:                     { label: 'CDB',                grupo: 'renda-fixa-tributavel' },
    [TipoAtivoCalculadora.Rdb]:                     { label: 'RDB',                grupo: 'renda-fixa-tributavel' },
    [TipoAtivoCalculadora.TesouroSelic]:            { label: 'Tesouro Selic',      grupo: 'renda-fixa-tributavel' },
    [TipoAtivoCalculadora.TesouroIpca]:             { label: 'Tesouro IPCA+',      grupo: 'renda-fixa-tributavel' },
    [TipoAtivoCalculadora.TesouroPrefixado]:        { label: 'Tesouro Prefixado',  grupo: 'renda-fixa-tributavel' },
    [TipoAtivoCalculadora.Lci]:                     { label: 'LCI',                grupo: 'renda-fixa-isenta' },
    [TipoAtivoCalculadora.Lca]:                     { label: 'LCA',                grupo: 'renda-fixa-isenta' },
    [TipoAtivoCalculadora.Acao]:                    { label: 'Ação',               grupo: 'renda-variavel' },
    [TipoAtivoCalculadora.Fii]:                     { label: 'FII',                grupo: 'renda-variavel' },
    [TipoAtivoCalculadora.Cripto]:                  { label: 'Criptomoeda',        grupo: 'renda-variavel' },
    [TipoAtivoCalculadora.FundoRendaFixaLongoPrazo]: { label: 'Fundo RF Longo Prazo', grupo: 'fundos' },
    [TipoAtivoCalculadora.FundoRendaFixaCurtoPrazo]: { label: 'Fundo RF Curto Prazo', grupo: 'fundos' },
    [TipoAtivoCalculadora.FundoMultimercado]:        { label: 'Fundo Multimercado',   grupo: 'fundos' },
    [TipoAtivoCalculadora.FundoAcoes]:               { label: 'Fundo de Ações',       grupo: 'fundos' },
    [TipoAtivoCalculadora.Pgbl]:                     { label: 'PGBL',               grupo: 'previdencia' },
    [TipoAtivoCalculadora.Vgbl]:                     { label: 'VGBL',               grupo: 'previdencia' },
};

export const GRUPO_TIPO_ATIVO_LABEL: Record<GrupoTipoAtivo, string> = {
    'renda-fixa-tributavel': 'Renda Fixa Tributável',
    'renda-fixa-isenta': 'Renda Fixa Isenta',
    'renda-variavel': 'Renda Variável',
    'fundos': 'Fundos de Investimento',
    'previdencia': 'Previdência Privada',
};

export const GRUPOS_TIPO_ATIVO: GrupoTipoAtivo[] = [
    'renda-fixa-tributavel',
    'renda-fixa-isenta',
    'renda-variavel',
    'fundos',
    'previdencia',
];

export const tiposAtivoPorGrupo = (grupo: GrupoTipoAtivo): TipoAtivoCalculadora[] =>
    Object.entries(TIPO_ATIVO_CALCULADORA_META)
        .filter(([, meta]) => meta.grupo === grupo)
        .map(([tipo]) => Number(tipo) as TipoAtivoCalculadora);

/** Explica a regra de tributação de cada subtipo — usado como legenda no seletor. */
export const getTipoAtivoHint = (tipoAtivo: TipoAtivoCalculadora): string => {
    switch (tipoAtivo) {
        case TipoAtivoCalculadora.Lci:
        case TipoAtivoCalculadora.Lca:
            return 'Nenhum IR é descontado do rendimento.';
        case TipoAtivoCalculadora.Acao:
            return '15% de IR sobre o ganho de capital na venda; isento se o valor final simulado ficar abaixo de R$ 20.000.';
        case TipoAtivoCalculadora.Fii:
            return '20% de IR sobre o ganho de capital na venda de cotas, sem isenção por valor.';
        case TipoAtivoCalculadora.Cripto:
            return '15% de IR sobre o ganho de capital; isento se o valor final simulado ficar abaixo de R$ 35.000.';
        case TipoAtivoCalculadora.FundoAcoes:
            return '15% de IR sobre o ganho de capital no resgate, sem come-cotas e sem isenção por valor.';
        case TipoAtivoCalculadora.FundoRendaFixaLongoPrazo:
        case TipoAtivoCalculadora.FundoMultimercado:
            return 'Come-cotas semestral de 15% sobre o rendimento acumulado, com IR complementar (tabela regressiva) no resgate.';
        case TipoAtivoCalculadora.FundoRendaFixaCurtoPrazo:
            return 'Come-cotas semestral de 20% sobre o rendimento acumulado, com IR complementar (tabela regressiva) no resgate.';
        case TipoAtivoCalculadora.Pgbl:
            return 'Regime regressivo definitivo: IR sobre o valor total resgatado (as contribuições foram deduzidas do IR na época do aporte).';
        case TipoAtivoCalculadora.Vgbl:
            return 'Regime regressivo definitivo: IR apenas sobre o rendimento, como na renda fixa.';
        default:
            return 'Aplica a tabela regressiva de IR (22,5% a 15%) e IOF regressivo sobre o rendimento, conforme o prazo.';
    }
};
