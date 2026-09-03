import { SegmentedControl } from '../../Shared/ui';
import './campos.css';

/**
 * ⚠ Não confundir com `CampoTaxaRendimento`/`TaxaRendimentoValue`. Este
 * campo é exclusivo do Financiamento: `PeriodicidadeTaxa` descreve se a taxa
 * do contrato foi informada ao mês ou ao ano — um conceito de periodicidade,
 * não de fonte de rendimento (Selic/CDI/manual). Os dois nunca devem virar
 * o mesmo componente nem o mesmo tipo.
 */
export type PeriodicidadeTaxa = 'mensal' | 'anual';

interface CampoTaxaPeriodicaProps {
    id: string;
    label?: string;
    periodicidade: PeriodicidadeTaxa;
    onChangePeriodicidade: (p: PeriodicidadeTaxa) => void;
    valor: string;
    onChangeValor: (v: string) => void;
    disabled?: boolean;
    erro?: string | null;
    hint?: string;
}

export function CampoTaxaPeriodica({
    id, label = 'Taxa de juros do contrato', periodicidade, onChangePeriodicidade, valor, onChangeValor, disabled, erro, hint,
}: CampoTaxaPeriodicaProps) {
    return (
        <div className="campo-form-group">
            <label>{label}</label>
            <SegmentedControl
                value={periodicidade}
                onChange={onChangePeriodicidade}
                ariaLabel="Unidade da taxa de juros"
                opcoes={[
                    { valor: 'mensal', rotulo: '% ao mês' },
                    { valor: 'anual', rotulo: '% ao ano' },
                ]}
                disabled={disabled}
            />
            <input
                id={id}
                className="campo-taxa-manual-input"
                type="text"
                inputMode="decimal"
                placeholder={periodicidade === 'mensal' ? 'Ex: 1,5 (% ao mês)' : 'Ex: 15 (% ao ano)'}
                value={valor}
                onChange={e => onChangeValor(e.target.value)}
                disabled={disabled}
                aria-invalid={!!erro}
                aria-describedby={erro ? `${id}-erro` : undefined}
            />
            {hint && <p className="campo-hint">{hint}</p>}
            {erro && <span id={`${id}-erro`} className="campo-erro">{erro}</span>}
        </div>
    );
}
