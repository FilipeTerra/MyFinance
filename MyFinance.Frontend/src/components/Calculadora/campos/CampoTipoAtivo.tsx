import type { TipoAtivoCalculadora } from '../../../types/TipoAtivoCalculadora';
import {
    GRUPOS_TIPO_ATIVO,
    GRUPO_TIPO_ATIVO_LABEL,
    TIPO_ATIVO_CALCULADORA_META,
    tiposAtivoPorGrupo,
    getTipoAtivoHint,
} from '../tipoAtivoCalculadoraMeta';
import './campos.css';

interface CampoTipoAtivoProps {
    id: string;
    value: TipoAtivoCalculadora;
    onChange: (tipo: TipoAtivoCalculadora) => void;
    mostrarHint?: boolean;
    disabled?: boolean;
}

/**
 * `<select>` agrupado por regime tributário — substitui os 16 botões em 5
 * grupos do antigo `SeletorTipoAtivo` (~380px de formulário → ~90px).
 */
export function CampoTipoAtivo({ id, value, onChange, mostrarHint = true, disabled }: CampoTipoAtivoProps) {
    return (
        <div className="campo-form-group">
            <label htmlFor={id}>Tipo de ativo</label>
            <select
                id={id}
                value={value}
                onChange={e => onChange(Number(e.target.value) as TipoAtivoCalculadora)}
                disabled={disabled}
            >
                {GRUPOS_TIPO_ATIVO.map(grupo => (
                    <optgroup key={grupo} label={GRUPO_TIPO_ATIVO_LABEL[grupo]}>
                        {tiposAtivoPorGrupo(grupo).map(tipo => (
                            <option key={tipo} value={tipo}>
                                {TIPO_ATIVO_CALCULADORA_META[tipo].label}
                            </option>
                        ))}
                    </optgroup>
                ))}
            </select>
            {mostrarHint && <p className="campo-hint">{getTipoAtivoHint(value)}</p>}
        </div>
    );
}
