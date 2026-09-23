import { useId } from 'react';
import './Switch.css';

interface SwitchProps {
    checked: boolean;
    onChange: (checked: boolean) => void;
    /** Texto visível ao lado do controle; vira o rótulo acessível do switch. */
    label: string;
    /** Linha de apoio abaixo do rótulo, associada via aria-describedby. */
    descricao?: string;
    disabled?: boolean;
    id?: string;
}

/**
 * Liga/desliga booleano. O `SegmentedControl` não serve aqui: ele é um
 * radiogroup (escolher entre N opções), e forçar um "Sim/Não" nele anunciaria
 * duas opções mutuamente exclusivas onde existe um único estado.
 *
 * `<button role="switch">` já responde a Espaço e Enter por ser um botão nativo,
 * sem precisar de handler de teclado próprio.
 */
export function Switch({ checked, onChange, label, descricao, disabled = false, id }: SwitchProps) {
    const idGerado = useId();
    const idRotulo = `${id ?? idGerado}-rotulo`;
    const idDescricao = `${id ?? idGerado}-descricao`;

    return (
        <div className="switch-wrapper">
            <button
                type="button"
                role="switch"
                id={id}
                aria-checked={checked}
                aria-labelledby={idRotulo}
                aria-describedby={descricao ? idDescricao : undefined}
                className={`switch-control${checked ? ' switch-control--on' : ''}`}
                onClick={() => onChange(!checked)}
                disabled={disabled}
            >
                <span className="switch-trilho" aria-hidden="true">
                    <span className="switch-bolinha" />
                </span>
                <span className="switch-textos">
                    <span className="switch-rotulo" id={idRotulo}>{label}</span>
                    {descricao && (
                        <span className="switch-descricao" id={idDescricao}>{descricao}</span>
                    )}
                </span>
            </button>
        </div>
    );
}
