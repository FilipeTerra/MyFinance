import { useState } from 'react';
import { metaReversaService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { AporteNecessarioResponseDto, PrazoNecessarioResponseDto } from '../../types/MetaReversa';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { formatCurrency, parseCurrency } from './calculadoraUtils';
import { prazoParaMeses, validarPrazo, validarTaxaRendimento, parametrosTaxa, formatPrazo } from './calculadoraValidacao';
import type { PrazoValue, TaxaRendimentoValue } from './calculadoraTypes';
import { ResultadoProjecaoDetalhado } from './ResultadoProjecaoDetalhado';
import { CampoMoeda } from './campos/CampoMoeda';
import { CampoPrazo } from './campos/CampoPrazo';
import { CampoTaxaRendimento } from './campos/CampoTaxaRendimento';
import { CampoTipoAtivo } from './campos/CampoTipoAtivo';
import { FormFooterCalculadora } from './campos/FormFooterCalculadora';
import { ResultadoSecao } from './campos/ResultadoSecao';
import { SegmentedControl, Alerta } from '../Shared/ui';
import { useResultadoFoco } from '../../hooks/useResultadoFoco';
import { useErrosFormulario } from '../../hooks/useErrosFormulario';

type ObjetivoModo = 'aporte' | 'prazo';
type CampoErro = 'valorAlvo' | 'prazo' | 'aporteMensal' | 'taxa';
const ID_POR_CAMPO: Record<CampoErro, string> = {
    valorAlvo: 'metaValorAlvo',
    prazo: 'metaPrazo',
    aporteMensal: 'metaAporteMensal',
    taxa: 'meta-taxa-valor',
};

export function MetaReversaCalculadora() {
    const [objetivo, setObjetivo] = useState<ObjetivoModo>('aporte');
    const [aporteInicial, setAporteInicial] = useState('');
    const [valorAlvo, setValorAlvo] = useState('');
    const [prazo, setPrazo] = useState<PrazoValue>({ valor: '10', unidade: 'anos' });
    const [aporteMensal, setAporteMensal] = useState('');
    const [taxa, setTaxa] = useState<TaxaRendimentoValue>({ modo: 'selic', taxaManual: '', percentualCdi: '100' });
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);

    const [isLoading, setIsLoading] = useState(false);
    const [resultadoAporte, setResultadoAporte] = useState<AporteNecessarioResponseDto | null>(null);
    const [resultadoPrazo, setResultadoPrazo] = useState<PrazoNecessarioResponseDto | null>(null);
    const { erros, erroGeral, limpar, limparTudo, setErroGeral, definirEFocar } = useErrosFormulario<CampoErro>();
    const resultadoRef = useResultadoFoco(resultadoAporte ?? resultadoPrazo);

    const prazoMeses = prazoParaMeses(prazo);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        limparTudo();
        setResultadoAporte(null);
        setResultadoPrazo(null);

        const novosErros: Partial<Record<CampoErro, string>> = {};

        const valorAlvoNumero = parseCurrency(valorAlvo);
        if (!valorAlvoNumero || valorAlvoNumero <= 0) novosErros.valorAlvo = 'Informe um valor-alvo válido maior que zero.';

        const erroTaxa = validarTaxaRendimento(taxa);
        if (erroTaxa) novosErros.taxa = erroTaxa;

        if (objetivo === 'aporte') {
            const erroPrazo = validarPrazo(prazo);
            if (erroPrazo) novosErros.prazo = erroPrazo;
        } else {
            const aporteMensalNumero = parseCurrency(aporteMensal);
            if (!aporteMensalNumero || aporteMensalNumero <= 0) novosErros.aporteMensal = 'Informe um aporte mensal válido maior que zero.';
        }

        if (Object.keys(novosErros).length > 0) {
            definirEFocar(novosErros, ID_POR_CAMPO);
            return;
        }

        const taxaConfig = { ...parametrosTaxa(taxa), tipoAtivo };

        setIsLoading(true);
        try {
            if (objetivo === 'aporte') {
                const data = await metaReversaService.calcularAporteNecessario({
                    aporteInicial: parseCurrency(aporteInicial),
                    valorAlvo: valorAlvoNumero,
                    prazoMeses,
                    ...taxaConfig,
                });
                setResultadoAporte(data);
            } else {
                const data = await metaReversaService.calcularPrazoNecessario({
                    aporteInicial: parseCurrency(aporteInicial),
                    aporteMensal: parseCurrency(aporteMensal),
                    valorAlvo: valorAlvoNumero,
                    ...taxaConfig,
                });
                setResultadoPrazo(data);
            }
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroGeral(axiosError.response?.data?.message || 'Não foi possível calcular a meta reversa. Tente novamente.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="proj-container">
            <form className="proj-form" onSubmit={handleSubmit}>
                <div className="campo-form-group">
                    <label>O que você quer descobrir?</label>
                    <SegmentedControl
                        value={objetivo}
                        onChange={setObjetivo}
                        ariaLabel="Objetivo da meta reversa"
                        full
                        opcoes={[
                            { valor: 'aporte', rotulo: 'Quanto preciso aportar por mês?' },
                            { valor: 'prazo', rotulo: 'Quanto tempo vou levar?' },
                        ]}
                        disabled={isLoading}
                    />
                </div>

                <div className="proj-form-row">
                    <CampoMoeda id="metaAporteInicial" label="Aporte inicial (R$)" value={aporteInicial} onChange={setAporteInicial} disabled={isLoading} />
                    <CampoMoeda
                        id="metaValorAlvo"
                        label="Valor-alvo líquido (R$)"
                        value={valorAlvo}
                        onChange={v => { setValorAlvo(v); limpar('valorAlvo'); }}
                        disabled={isLoading}
                        erro={erros.valorAlvo}
                    />
                </div>

                {objetivo === 'aporte' ? (
                    <CampoPrazo
                        id="metaPrazo"
                        value={prazo}
                        onChange={v => { setPrazo(v); limpar('prazo'); }}
                        disabled={isLoading}
                        erro={erros.prazo}
                    />
                ) : (
                    <CampoMoeda
                        id="metaAporteMensal"
                        label="Aporte mensal (R$)"
                        value={aporteMensal}
                        onChange={v => { setAporteMensal(v); limpar('aporteMensal'); }}
                        disabled={isLoading}
                        erro={erros.aporteMensal}
                    />
                )}

                <CampoTaxaRendimento
                    idPrefix="meta"
                    value={taxa}
                    onChange={v => { setTaxa(v); limpar('taxa'); }}
                    disabled={isLoading}
                    erro={erros.taxa}
                />

                <CampoTipoAtivo id="metaTipoAtivo" value={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />

                <FormFooterCalculadora erro={erroGeral} isLoading={isLoading} rotulo="Calcular" />
            </form>

            {resultadoAporte && (
                <ResultadoSecao resultadoRef={resultadoRef} titulo="Aporte necessário">
                    <div className="proj-result-stats">
                        <div className="proj-result-stat proj-result-stat--highlight">
                            <span className="proj-result-stat-value">{formatCurrency(resultadoAporte.aporteMensalNecessario)}</span>
                            <span className="proj-result-stat-label">Aporte mensal necessário</span>
                        </div>
                    </div>
                    <ResultadoProjecaoDetalhado resultado={resultadoAporte.projecao} prazoMeses={prazoMeses} />
                </ResultadoSecao>
            )}

            {resultadoPrazo && (
                resultadoPrazo.atingivel && resultadoPrazo.projecao && resultadoPrazo.prazoMesesNecessario ? (
                    <ResultadoSecao resultadoRef={resultadoRef} titulo="Prazo necessário">
                        <div className="proj-result-stats">
                            <div className="proj-result-stat proj-result-stat--highlight">
                                <span className="proj-result-stat-value">{formatPrazo(resultadoPrazo.prazoMesesNecessario)}</span>
                                <span className="proj-result-stat-label">Prazo necessário</span>
                            </div>
                        </div>
                        <ResultadoProjecaoDetalhado resultado={resultadoPrazo.projecao} prazoMeses={resultadoPrazo.prazoMesesNecessario} />
                    </ResultadoSecao>
                ) : (
                    // Resultado legítimo, não uma falha de validação — antes usava o
                    // mesmo estilo de erro do formulário, o que confundia as duas coisas.
                    <div ref={resultadoRef} tabIndex={-1}>
                        <Alerta variante="aviso">
                            Com esse aporte e taxa, a meta não é atingível em até 50 anos. Aumente o aporte mensal ou revise o valor-alvo.
                        </Alerta>
                    </div>
                )
            )}
        </div>
    );
}
