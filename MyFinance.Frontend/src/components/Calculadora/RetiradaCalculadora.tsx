import { useState } from 'react';
import { retiradaService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import type { RetiradaResponseDto } from '../../types/Retirada';
import { TipoAtivoCalculadora } from '../../types/TipoAtivoCalculadora';
import { parseCurrency } from './calculadoraUtils';
import { prazoParaMeses, validarPrazo, validarTaxaRendimento, parametrosTaxa } from './calculadoraValidacao';
import type { PrazoValue, TaxaRendimentoValue } from './calculadoraTypes';
import { ResultadoRetiradaDetalhado } from './ResultadoRetiradaDetalhado';
import { CampoMoeda } from './campos/CampoMoeda';
import { CampoPrazo } from './campos/CampoPrazo';
import { CampoTaxaRendimento } from './campos/CampoTaxaRendimento';
import { CampoTipoAtivo } from './campos/CampoTipoAtivo';
import { FormFooterCalculadora } from './campos/FormFooterCalculadora';
import { ResultadoSecao } from './campos/ResultadoSecao';
import { SegmentedControl } from '../Shared/ui';
import { useResultadoFoco } from '../../hooks/useResultadoFoco';
import { useErrosFormulario } from '../../hooks/useErrosFormulario';

type ObjetivoModo = 'saque' | 'duracao';
type CampoErro = 'saldoInicial' | 'baseCusto' | 'prazo' | 'saqueMensal' | 'taxa';
const ID_POR_CAMPO: Record<CampoErro, string> = {
    saldoInicial: 'retSaldoInicial',
    baseCusto: 'retBaseCusto',
    prazo: 'retPrazo',
    saqueMensal: 'retSaqueMensal',
    taxa: 'ret-taxa-valor',
};

export function RetiradaCalculadora() {
    const [objetivo, setObjetivo] = useState<ObjetivoModo>('saque');
    const [saldoInicial, setSaldoInicial] = useState('');
    const [baseCustoInicial, setBaseCustoInicial] = useState('');
    const [prazo, setPrazo] = useState<PrazoValue>({ valor: '30', unidade: 'anos' });
    const [saqueMensal, setSaqueMensal] = useState('');
    const [taxa, setTaxa] = useState<TaxaRendimentoValue>({ modo: 'selic', taxaManual: '', percentualCdi: '100' });
    const [tipoAtivo, setTipoAtivo] = useState<TipoAtivoCalculadora>(TipoAtivoCalculadora.TesouroSelic);

    const [isLoading, setIsLoading] = useState(false);
    const [resultado, setResultado] = useState<RetiradaResponseDto | null>(null);
    const { erros, erroGeral, limpar, limparTudo, setErroGeral, definirEFocar } = useErrosFormulario<CampoErro>();
    const resultadoRef = useResultadoFoco(resultado);

    const prazoMeses = prazoParaMeses(prazo);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        limparTudo();
        setResultado(null);

        const novosErros: Partial<Record<CampoErro, string>> = {};

        const saldoInicialNumero = parseCurrency(saldoInicial);
        const baseCustoNumero = parseCurrency(baseCustoInicial);
        if (!saldoInicialNumero || saldoInicialNumero <= 0) novosErros.saldoInicial = 'Informe um saldo inicial válido maior que zero.';
        if (baseCustoNumero < 0 || baseCustoNumero > saldoInicialNumero) {
            novosErros.baseCusto = 'A base de custo (total já aportado) deve estar entre zero e o saldo inicial.';
        }

        const erroTaxa = validarTaxaRendimento(taxa);
        if (erroTaxa) novosErros.taxa = erroTaxa;

        if (objetivo === 'saque') {
            const erroPrazo = validarPrazo(prazo);
            if (erroPrazo) novosErros.prazo = 'Informe um prazo de retirada válido maior que zero.';
        } else {
            const saqueMensalNumero = parseCurrency(saqueMensal);
            if (!saqueMensalNumero || saqueMensalNumero <= 0) novosErros.saqueMensal = 'Informe um saque mensal válido maior que zero.';
        }

        if (Object.keys(novosErros).length > 0) {
            definirEFocar(novosErros, ID_POR_CAMPO);
            return;
        }

        const taxaConfig = { ...parametrosTaxa(taxa), tipoAtivo };

        setIsLoading(true);
        try {
            if (objetivo === 'saque') {
                const data = await retiradaService.calcularSaqueSustentavel({
                    saldoInicial: saldoInicialNumero,
                    baseCustoInicial: baseCustoNumero,
                    prazoMeses,
                    ...taxaConfig,
                });
                setResultado(data);
            } else {
                const data = await retiradaService.calcularDuracao({
                    saldoInicial: saldoInicialNumero,
                    baseCustoInicial: baseCustoNumero,
                    saqueMensal: parseCurrency(saqueMensal),
                    ...taxaConfig,
                });
                setResultado(data);
            }
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErroGeral(axiosError.response?.data?.message || 'Não foi possível calcular a retirada. Tente novamente.');
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
                        ariaLabel="Objetivo da fase de retirada"
                        full
                        opcoes={[
                            { valor: 'saque', rotulo: 'Quanto posso sacar por mês?' },
                            { valor: 'duracao', rotulo: 'Quanto tempo meu saldo dura?' },
                        ]}
                        disabled={isLoading}
                    />
                </div>

                <div className="proj-form-row">
                    <CampoMoeda
                        id="retSaldoInicial"
                        label="Saldo inicial (R$)"
                        value={saldoInicial}
                        onChange={v => { setSaldoInicial(v); limpar('saldoInicial'); }}
                        disabled={isLoading}
                        erro={erros.saldoInicial}
                    />
                    <CampoMoeda
                        id="retBaseCusto"
                        label="Total já aportado (R$)"
                        value={baseCustoInicial}
                        onChange={v => { setBaseCustoInicial(v); limpar('baseCusto'); }}
                        disabled={isLoading}
                        erro={erros.baseCusto}
                        hint="Usado para calcular quanto de cada saque é ganho tributável e quanto é apenas devolução do que você mesmo investiu."
                    />
                </div>

                {objetivo === 'saque' ? (
                    <CampoPrazo
                        id="retPrazo"
                        label="Prazo de retirada desejado"
                        value={prazo}
                        onChange={v => { setPrazo(v); limpar('prazo'); }}
                        disabled={isLoading}
                        erro={erros.prazo}
                    />
                ) : (
                    <CampoMoeda
                        id="retSaqueMensal"
                        label="Saque mensal desejado (R$)"
                        value={saqueMensal}
                        onChange={v => { setSaqueMensal(v); limpar('saqueMensal'); }}
                        disabled={isLoading}
                        erro={erros.saqueMensal}
                    />
                )}

                <CampoTaxaRendimento
                    idPrefix="ret"
                    label="Taxa de retorno na aposentadoria"
                    value={taxa}
                    onChange={v => { setTaxa(v); limpar('taxa'); }}
                    disabled={isLoading}
                    erro={erros.taxa}
                />

                <CampoTipoAtivo id="retTipoAtivo" value={tipoAtivo} onChange={setTipoAtivo} disabled={isLoading} />
                <p className="campo-hint">
                    O IR de cada saque é calculado proporcionalmente ao ganho embutido nele (base de custo
                    amortizada mês a mês), assumindo a alíquota mínima da tabela regressiva — dinheiro em
                    aposentadoria já costuma estar investido há anos.
                </p>

                <FormFooterCalculadora erro={erroGeral} isLoading={isLoading} rotulo="Calcular" />
            </form>

            {resultado && (
                <ResultadoSecao resultadoRef={resultadoRef}>
                    <ResultadoRetiradaDetalhado resultado={resultado} />
                </ResultadoSecao>
            )}
        </div>
    );
}
