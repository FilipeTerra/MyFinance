import { maskCurrency } from '../calculadoraUtils';
import './campos.css';

interface CampoMoedaProps {
    id: string;
    label: string;
    value: string;
    onChange: (mascarado: string) => void;
    placeholder?: string;
    hint?: string;
    erro?: string | null;
    obrigatorio?: boolean;
    disabled?: boolean;
}

/** Campo de valor em reais — aplica a máscara e não deixa cada formulário reimplementar `maskCurrency`. */
export function CampoMoeda({
    id, label, value, onChange, placeholder = '0,00', hint, erro, obrigatorio, disabled,
}: CampoMoedaProps) {
    return (
        <div className="campo-form-group">
            <label htmlFor={id}>{label}{obrigatorio && <span className="campo-obrigatorio"> *</span>}</label>
            <input
                id={id}
                type="text"
                inputMode="numeric"
                placeholder={placeholder}
                value={value}
                onChange={e => onChange(maskCurrency(e.target.value))}
                disabled={disabled}
                aria-invalid={!!erro}
                aria-describedby={erro ? `${id}-erro` : undefined}
                className={erro ? 'campo-input--erro' : undefined}
            />
            {hint && <p className="campo-hint">{hint}</p>}
            {erro && <span id={`${id}-erro`} className="campo-erro">{erro}</span>}
        </div>
    );
}
