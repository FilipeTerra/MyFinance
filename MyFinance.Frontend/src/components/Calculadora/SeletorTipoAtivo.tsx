import {
    GRUPOS_TIPO_ATIVO,
    GRUPO_TIPO_ATIVO_LABEL,
    TIPO_ATIVO_CALCULADORA_META,
    tiposAtivoPorGrupo,
    getTipoAtivoHint,
    type SeletorTipoAtivoProps,
} from './tipoAtivoCalculadoraMeta';

/** Seletor agrupado de tipo de ativo, com legenda da regra de tributação — usado no cenário único e na meta reversa. */
export function SeletorTipoAtivo({ tipoAtivo, onChange, disabled }: SeletorTipoAtivoProps) {
    return (
        <div className="proj-form-group">
            <label>Tipo de ativo</label>
            {GRUPOS_TIPO_ATIVO.map(grupo => (
                <div key={grupo} className="proj-tipo-ativo-grupo">
                    <span className="proj-tipo-ativo-grupo-label">{GRUPO_TIPO_ATIVO_LABEL[grupo]}</span>
                    <div
                        className="proj-toggle-group proj-toggle-group--full"
                        role="radiogroup"
                        aria-label={GRUPO_TIPO_ATIVO_LABEL[grupo]}
                    >
                        {tiposAtivoPorGrupo(grupo).map(tipo => (
                            <button
                                key={tipo}
                                type="button"
                                role="radio"
                                aria-checked={tipoAtivo === tipo}
                                className={`proj-toggle-btn${tipoAtivo === tipo ? ' proj-toggle-btn--active' : ''}`}
                                onClick={() => onChange(tipo)}
                                disabled={disabled}
                            >
                                {TIPO_ATIVO_CALCULADORA_META[tipo].label}
                            </button>
                        ))}
                    </div>
                </div>
            ))}
            <p className="proj-hint">{getTipoAtivoHint(tipoAtivo)}</p>
        </div>
    );
}
