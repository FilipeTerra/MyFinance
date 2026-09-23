import type { CorteSugeridoDto } from '../../types/SugestaoAporte';
import { formatCurrency } from './calculadoraUtils';

interface PlanoDeCorteProps {
    cortes: CorteSugeridoDto[];
}

/**
 * Tabela categoria → corte → gasto restante. No celular o CSS troca o `display`
 * de tabela para bloco (cartões), e é por isso que cada célula declara `role`
 * e `data-rotulo`: sem eles o leitor de tela perderia a relação valor/coluna.
 */
export function PlanoDeCorte({ cortes }: PlanoDeCorteProps) {
    const total = cortes.reduce((soma, corte) => soma + corte.valorCorte, 0);

    return (
        <div className="plano-corte">
            <h4 className="sugestao-subtitulo">De onde tirar</h4>
            <table className="plano-corte-tabela">
                <caption className="plano-corte-caption">
                    Corte mensal sugerido por categoria, somando {formatCurrency(total)}
                </caption>
                <thead className="plano-corte-head">
                    <tr>
                        <th scope="col">Categoria</th>
                        <th scope="col">Gasto hoje</th>
                        <th scope="col">Cortar</th>
                        <th scope="col">Passa a gastar</th>
                    </tr>
                </thead>
                <tbody>
                    {cortes.map(corte => (
                        <tr key={corte.categoryId} className="plano-corte-linha">
                            <td role="cell" data-rotulo="Categoria" className="plano-corte-categoria">
                                {corte.categoryName}
                            </td>
                            <td role="cell" data-rotulo="Gasto hoje">{formatCurrency(corte.gastoAtual)}</td>
                            <td role="cell" data-rotulo="Cortar" className="plano-corte-valor">
                                −{formatCurrency(corte.valorCorte)}
                            </td>
                            <td role="cell" data-rotulo="Passa a gastar">{formatCurrency(corte.gastoDepoisDoCorte)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
