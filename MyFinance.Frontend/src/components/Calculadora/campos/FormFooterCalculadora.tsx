import './campos.css';

interface FormFooterProps {
    /** Erro de servidor/geral — slot separado dos erros de campo, para não confundir "falha de rede" com "campo inválido". */
    erro?: string | null;
    isLoading: boolean;
    rotulo: string;
    rotuloCarregando?: string;
    disabled?: boolean;
}

/** Rodapé de erro geral + botão de submit — bloco repetido em 5 formulários. */
export function FormFooterCalculadora({ erro, isLoading, rotulo, rotuloCarregando = 'Calculando...', disabled }: FormFooterProps) {
    return (
        <>
            {erro && (
                <span className="campo-erro campo-erro--geral" role="alert" aria-live="polite">{erro}</span>
            )}
            <button type="submit" className="campo-btn-submit" disabled={isLoading || disabled}>
                {isLoading ? rotuloCarregando : rotulo}
            </button>
        </>
    );
}
