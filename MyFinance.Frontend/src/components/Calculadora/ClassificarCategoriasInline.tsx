import { useState } from 'react';
import { categoryService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import { ExpenseNature } from '../../types/ExpenseNature';
import type { CategoriaNaoClassificadaDto } from '../../types/SugestaoAporte';
import { formatCurrency } from './calculadoraUtils';
import { Alerta } from '../Shared/ui';

interface ClassificarCategoriasInlineProps {
    categorias: CategoriaNaoClassificadaDto[];
    /** Chamado após a gravação do lote, para recarregar a sugestão com o plano de corte. */
    onClassificado: () => void;
}

/**
 * Classificação das categorias feita aqui mesmo, dentro da sugestão: é o único
 * lugar do app onde a classificação tem consequência visível na hora, e obrigar
 * o usuário a sair para uma tela de gestão de categorias perderia o contexto.
 *
 * As escolhas ficam em memória e sobem em um único PUT — a API valida o lote
 * inteiro antes de gravar qualquer linha.
 */
export function ClassificarCategoriasInline({ categorias, onClassificado }: ClassificarCategoriasInlineProps) {
    const [escolhas, setEscolhas] = useState<Record<string, ExpenseNature>>({});
    const [isSalvando, setIsSalvando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);

    const totalEscolhido = Object.keys(escolhas).length;

    const escolher = (categoryId: string, nature: ExpenseNature) => {
        setErro(null);
        setEscolhas(anterior => ({ ...anterior, [categoryId]: nature }));
    };

    const salvar = async () => {
        if (totalEscolhido === 0) return;

        setIsSalvando(true);
        setErro(null);
        try {
            await categoryService.atualizarNaturezas({
                items: Object.entries(escolhas).map(([categoryId, nature]) => ({ categoryId, nature })),
            });
            setEscolhas({});
            onClassificado();
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErro(axiosError.response?.data?.message || 'Não foi possível salvar a classificação. Tente novamente.');
        } finally {
            setIsSalvando(false);
        }
    };

    return (
        <div className="sugestao-classificar">
            <h4 className="sugestao-subtitulo">Classifique seus gastos</h4>
            <p className="sugestao-texto-apoio">
                Marque o que é essencial e o que você aceitaria cortar. Só as categorias
                marcadas como supérfluas entram no plano de corte.
            </p>

            <ul className="sugestao-classificar-lista">
                {categorias.map(categoria => {
                    const escolha = escolhas[categoria.categoryId];
                    return (
                        <li key={categoria.categoryId} className="sugestao-classificar-item">
                            <div className="sugestao-classificar-info">
                                <span className="sugestao-classificar-nome">{categoria.categoryName}</span>
                                <span className="sugestao-classificar-valor">
                                    {formatCurrency(categoria.gastoMensalMedio)}/mês
                                </span>
                            </div>
                            <div
                                className="sugestao-classificar-botoes"
                                role="group"
                                aria-label={`Classificação de ${categoria.categoryName}`}
                            >
                                <button
                                    type="button"
                                    className={`sugestao-chip${escolha === ExpenseNature.Essencial ? ' sugestao-chip--essencial' : ''}`}
                                    aria-pressed={escolha === ExpenseNature.Essencial}
                                    onClick={() => escolher(categoria.categoryId, ExpenseNature.Essencial)}
                                    disabled={isSalvando}
                                >
                                    Essencial
                                </button>
                                <button
                                    type="button"
                                    className={`sugestao-chip${escolha === ExpenseNature.Discricionario ? ' sugestao-chip--superfluo' : ''}`}
                                    aria-pressed={escolha === ExpenseNature.Discricionario}
                                    onClick={() => escolher(categoria.categoryId, ExpenseNature.Discricionario)}
                                    disabled={isSalvando}
                                >
                                    Supérfluo
                                </button>
                            </div>
                        </li>
                    );
                })}
            </ul>

            {erro && <Alerta variante="erro">{erro}</Alerta>}

            <button
                type="button"
                className="sugestao-btn-primario"
                onClick={salvar}
                disabled={totalEscolhido === 0 || isSalvando}
            >
                {isSalvando
                    ? 'Salvando…'
                    : `Salvar classificação${totalEscolhido > 0 ? ` (${totalEscolhido})` : ''}`}
            </button>
        </div>
    );
}
