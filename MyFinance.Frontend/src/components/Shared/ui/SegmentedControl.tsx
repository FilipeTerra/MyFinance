import type { ReactNode } from 'react';
import { useRovingRadioGroup } from '../../../hooks/useRovingRadioGroup';
import './SegmentedControl.css';

export interface OpcaoSegmentedControl<T extends string> {
    valor: T;
    rotulo: string;
    icone?: ReactNode;
    desabilitado?: boolean;
}

interface SegmentedControlProps<T extends string> {
    value: T;
    onChange: (valor: T) => void;
    opcoes: OpcaoSegmentedControl<T>[];
    ariaLabel: string;
    /** pill = chips de formulário; segmented = trilho cinza (abas do dashboard); tabs = navegação com sublinhado (modos da calculadora). */
    variante?: 'pill' | 'segmented' | 'tabs';
    tamanho?: 'sm' | 'md';
    /** true → role="tablist"/"tab" com aria-controls; false (padrão) → role="radiogroup"/"radio". */
    semanticaTab?: boolean;
    idsPaineis?: Partial<Record<T, string>>;
    className?: string;
    /** Cresce para preencher a largura, quebrando linha (chips de formulário). */
    full?: boolean;
    /** overflow-x: auto com scroll-snap — evita estouro horizontal em telas estreitas. */
    rolavel?: boolean;
    disabled?: boolean;
}

/**
 * Substitui `.proj-toggle-group`, `.gastos-toggle-group`, `.dashboard-tabs`
 * e o seletor de modos da Calculadora — os quatro grupos de toggle que não
 * tinham roving tabindex nem tratamento de overflow em telas estreitas.
 */
export function SegmentedControl<T extends string>({
    value,
    onChange,
    opcoes,
    ariaLabel,
    variante = 'pill',
    tamanho = 'md',
    semanticaTab = false,
    idsPaineis,
    className,
    full = false,
    rolavel = false,
    disabled = false,
}: SegmentedControlProps<T>) {
    const valores = opcoes.map(o => o.valor);
    const { containerRef, handleKeyDown } = useRovingRadioGroup(valores, value, onChange, 'horizontal');

    const papelGrupo = semanticaTab ? 'tablist' : 'radiogroup';
    const papelItem = semanticaTab ? 'tab' : 'radio';

    const classes = [
        'seg-control',
        `seg-control--${variante}`,
        `seg-control--${tamanho}`,
        full && 'seg-control--full',
        rolavel && 'seg-control--rolavel',
        className,
    ].filter(Boolean).join(' ');

    return (
        <div ref={containerRef} className={classes} role={papelGrupo} aria-label={ariaLabel} onKeyDown={handleKeyDown}>
            {opcoes.map(opcao => {
                const ativo = opcao.valor === value;
                return (
                    <button
                        key={opcao.valor}
                        type="button"
                        role={papelItem}
                        aria-selected={semanticaTab ? ativo : undefined}
                        aria-checked={semanticaTab ? undefined : ativo}
                        aria-controls={semanticaTab ? idsPaineis?.[opcao.valor] : undefined}
                        tabIndex={ativo ? 0 : -1}
                        className={`seg-control-btn${ativo ? ' seg-control-btn--active' : ''}`}
                        onClick={() => onChange(opcao.valor)}
                        disabled={disabled || opcao.desabilitado}
                    >
                        {opcao.icone && (
                            <span className="seg-control-btn-icon" aria-hidden="true">{opcao.icone}</span>
                        )}
                        {opcao.rotulo}
                    </button>
                );
            })}
        </div>
    );
}
