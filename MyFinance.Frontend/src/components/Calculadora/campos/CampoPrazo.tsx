import { SegmentedControl } from '../../Shared/ui';
import type { PrazoUnidade, PrazoValue } from '../calculadoraTypes';
import './campos.css';

interface CampoPrazoProps {
    id: string;
    label?: string;
    value: PrazoValue;
    onChange: (valor: PrazoValue) => void;
    /** Ordem dos botões de unidade — o Financiamento historicamente mostrava "Meses" primeiro; os demais modos mostram "Anos" primeiro. */
    ordemUnidades?: PrazoUnidade[];
    min?: number;
    disabled?: boolean;
    erro?: string | null;
}

const ROTULO_UNIDADE: Record<PrazoUnidade, string> = { anos: 'Anos', meses: 'Meses' };

/** Campo de prazo (número + toggle Anos/Meses) — bloco de ~30 linhas que estava copiado em 5 lugares. */
export function CampoPrazo({
    id, label = 'Prazo', value, onChange, ordemUnidades = ['anos', 'meses'], min = 1, disabled, erro,
}: CampoPrazoProps) {
    return (
        <div className="campo-form-group">
            <label htmlFor={id}>{label}</label>
            <div className="campo-prazo-row">
                <input
                    id={id}
                    type="number"
                    min={min}
                    value={value.valor}
                    onChange={e => onChange({ ...value, valor: e.target.value })}
                    disabled={disabled}
                    aria-invalid={!!erro}
                    aria-describedby={erro ? `${id}-erro` : undefined}
                />
                <SegmentedControl
                    value={value.unidade}
                    onChange={unidade => onChange({ ...value, unidade })}
                    ariaLabel="Unidade do prazo"
                    opcoes={ordemUnidades.map(u => ({ valor: u, rotulo: ROTULO_UNIDADE[u] }))}
                    disabled={disabled}
                />
            </div>
            {erro && <span id={`${id}-erro`} className="campo-erro">{erro}</span>}
        </div>
    );
}
