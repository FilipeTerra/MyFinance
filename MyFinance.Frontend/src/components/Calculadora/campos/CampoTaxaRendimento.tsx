import { SegmentedControl } from '../../Shared/ui';
import type { TaxaRendimentoValue } from '../calculadoraTypes';
import './campos.css';

interface CampoTaxaRendimentoProps {
    idPrefix: string;
    label?: string;
    value: TaxaRendimentoValue;
    onChange: (valor: TaxaRendimentoValue) => void;
    /**
     * Renderiza as explicações de Selic/CDI abaixo do campo. Fica ligado por
     * padrão — antes só o Cenário único mostrava essas dicas; Meta reversa e
     * Retirada as omitiam, e o usuário nunca descobria que a taxa é buscada
     * ao vivo nesses dois modos.
     */
    mostrarHints?: boolean;
    /** Rótulos curtos ("Selic"/"% CDI"/"Manual") para caber nos cards compactos do Comparador. */
    compacto?: boolean;
    disabled?: boolean;
    erro?: string | null;
}

/** Seletor Selic / % do CDI / Manual — bloco de ~55 linhas que estava copiado em 3 lugares. */
export function CampoTaxaRendimento({
    idPrefix, label = 'Taxa de juros', value, onChange, mostrarHints = true, compacto = false, disabled, erro,
}: CampoTaxaRendimentoProps) {
    return (
        <div className="campo-form-group">
            <label>{label}</label>
            <SegmentedControl
                value={value.modo}
                onChange={modo => onChange({ ...value, modo })}
                ariaLabel="Fonte da taxa de juros"
                full
                opcoes={[
                    { valor: 'selic', rotulo: compacto ? 'Selic' : 'Tesouro Direto (Selic atual)' },
                    { valor: 'cdi', rotulo: '% do CDI' },
                    { valor: 'manual', rotulo: 'Taxa manual' },
                ]}
                disabled={disabled}
            />

            {value.modo === 'manual' && (
                <input
                    id={`${idPrefix}-taxa-valor`}
                    className="campo-taxa-manual-input"
                    type="text"
                    inputMode="decimal"
                    placeholder="Ex: 10,5 (% ao ano)"
                    value={value.taxaManual}
                    onChange={e => onChange({ ...value, taxaManual: e.target.value })}
                    disabled={disabled}
                    aria-invalid={!!erro}
                    aria-describedby={erro ? `${idPrefix}-taxa-erro` : undefined}
                />
            )}
            {value.modo === 'cdi' && (
                <input
                    id={`${idPrefix}-taxa-valor`}
                    className="campo-taxa-manual-input"
                    type="text"
                    inputMode="decimal"
                    placeholder="Ex: 100 (% do CDI)"
                    value={value.percentualCdi}
                    onChange={e => onChange({ ...value, percentualCdi: e.target.value })}
                    disabled={disabled}
                    aria-invalid={!!erro}
                    aria-describedby={erro ? `${idPrefix}-taxa-erro` : undefined}
                />
            )}

            {mostrarHints && value.modo === 'selic' && (
                <p className="campo-hint">A taxa Selic vigente é buscada automaticamente (Banco Central) no momento do cálculo.</p>
            )}
            {mostrarHints && value.modo === 'cdi' && (
                <p className="campo-hint">O CDI vigente é buscado automaticamente e multiplicado pelo percentual informado.</p>
            )}

            {erro && <span id={`${idPrefix}-taxa-erro`} className="campo-erro">{erro}</span>}
        </div>
    );
}
