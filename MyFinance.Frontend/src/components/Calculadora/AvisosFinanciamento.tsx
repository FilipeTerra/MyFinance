import type { AvisoFinanciamentoDto, SeveridadeAvisoFinanciamento } from '../../types/Financiamento';
import './AvisosFinanciamento.css';

const ICONE_POR_SEVERIDADE: Record<SeveridadeAvisoFinanciamento, string> = {
    Informativo: 'ℹ️',
    Atencao: '⚠️',
    Critico: '⛔',
};

const ROTULO_POR_SEVERIDADE: Record<SeveridadeAvisoFinanciamento, string> = {
    Informativo: 'Informativo',
    Atencao: 'Atenção',
    Critico: 'Crítico',
};

interface AvisosFinanciamentoProps {
    avisos: AvisoFinanciamentoDto[];
}

/**
 * Lista de avisos não bloqueantes da simulação (renda apertada, teto do MCMV,
 * etc.). Nunca impede nada — por isso `role="status"`, não `role="alert"`: são
 * avisos para o usuário conferir, não erros que exigem ação imediata.
 */
export function AvisosFinanciamento({ avisos }: AvisosFinanciamentoProps) {
    if (avisos.length === 0) return null;

    return (
        <ul className="fin-avisos" role="status" aria-live="polite">
            {avisos.map((aviso, idx) => (
                <li key={`${aviso.codigo}-${idx}`} className={`fin-aviso fin-aviso--${aviso.severidade.toLowerCase()}`}>
                    <span className="fin-aviso-icone" aria-hidden="true">{ICONE_POR_SEVERIDADE[aviso.severidade]}</span>
                    <span className="fin-aviso-texto">
                        <span className="fin-aviso-rotulo">{ROTULO_POR_SEVERIDADE[aviso.severidade]}:</span>{' '}
                        {aviso.mensagem}
                    </span>
                </li>
            ))}
        </ul>
    );
}
