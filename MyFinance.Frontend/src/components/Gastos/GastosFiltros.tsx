import { SegmentedControl } from '../Shared/ui';
import type { AccountResponseDto } from '../../types/AccountResponseDto';
import type { PeriodoPreset } from './gastosSelectors';
import './GastosFiltros.css';

const PRESETS: { valor: PeriodoPreset; rotulo: string }[] = [
    { valor: '3m', rotulo: '3 meses' },
    { valor: '6m', rotulo: '6 meses' },
    { valor: '12m', rotulo: '12 meses' },
    { valor: 'ano', rotulo: 'Este ano' },
    { valor: 'custom', rotulo: 'Personalizado' },
];

interface GastosFiltrosProps {
    preset: PeriodoPreset;
    onPresetChange: (preset: PeriodoPreset) => void;
    customStart: string;
    customEnd: string;
    onCustomStartChange: (data: string) => void;
    onCustomEndChange: (data: string) => void;
    accounts: AccountResponseDto[];
    accountId: string;
    onAccountChange: (id: string) => void;
    /** Já formatada por quem chama (ex.: "1 jun 2026 – 30 ago 2026 · 3 meses"). */
    legenda: string | null;
    /** Nota sobre mês parcial já formatada (ex.: "agosto ainda em curso"), ou null quando o período não termina em mês parcial. */
    avisoMesParcial: string | null;
}

/**
 * Barra de filtros fixa no topo da aba (sticky) — antes o usuário perdia de
 * vista o período ativo ao rolar a página. Também mostra o intervalo de
 * datas resolvido, que nunca aparecia na tela.
 */
export function GastosFiltros({
    preset, onPresetChange, customStart, customEnd, onCustomStartChange, onCustomEndChange,
    accounts, accountId, onAccountChange, legenda, avisoMesParcial,
}: GastosFiltrosProps) {
    return (
        <div className="gastos-filtros">
            <div className="gastos-filtros-linha">
                <SegmentedControl
                    value={preset}
                    onChange={onPresetChange}
                    ariaLabel="Período"
                    opcoes={PRESETS.map(p => ({ valor: p.valor, rotulo: p.rotulo }))}
                    rolavel
                />

                {preset === 'custom' && (
                    <div className="gastos-custom-range">
                        <input
                            type="date"
                            value={customStart}
                            max={customEnd || undefined}
                            onChange={e => onCustomStartChange(e.target.value)}
                            aria-label="Data inicial"
                        />
                        <span className="gastos-custom-range-sep">→</span>
                        <input
                            type="date"
                            value={customEnd}
                            min={customStart || undefined}
                            onChange={e => onCustomEndChange(e.target.value)}
                            aria-label="Data final"
                        />
                    </div>
                )}

                {accounts.length > 0 && (
                    <select
                        className="gastos-account-select"
                        value={accountId}
                        onChange={e => onAccountChange(e.target.value)}
                        aria-label="Conta"
                    >
                        <option value="">Todas as contas</option>
                        {accounts.map(a => (
                            <option key={a.id} value={a.id}>{a.name}</option>
                        ))}
                    </select>
                )}
            </div>

            {legenda && (
                <p className="gastos-filtros-legenda">
                    {legenda}
                    {avisoMesParcial && <span className="gastos-filtros-aviso"> · {avisoMesParcial}</span>}
                </p>
            )}
        </div>
    );
}
