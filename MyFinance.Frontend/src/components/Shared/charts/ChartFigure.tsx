import type { ReactElement } from 'react';
import './ChartFigure.css';

interface ChartFigureProps {
    titulo: string;
    /** Descrição textual do gráfico para leitores de tela (ex.: "Barras de receita e despesa por mês, jun–ago 2026"). */
    descricao: string;
    /** Tabela de dados equivalente, só para leitores de tela — cobre o que hoje só existe no tooltip (mouse-only). */
    dadosTabela?: { colunas: string[]; linhas: (string | number)[][] };
    altura?: number;
    children: ReactElement;
}

/**
 * Embrulha um gráfico recharts em `<figure role="img">` com `aria-label` e
 * uma tabela `sr-only` equivalente — nenhum gráfico do app tinha isso, e a
 * Comparação de períodos codificava direção só por cor, com o valor exato
 * existindo apenas no tooltip (inacessível por teclado).
 */
export function ChartFigure({ titulo, descricao, dadosTabela, altura, children }: ChartFigureProps) {
    return (
        <figure className="chart-figure" role="img" aria-label={descricao} style={altura ? { height: altura } : undefined}>
            <figcaption className="sr-only">{titulo} — {descricao}</figcaption>
            {children}
            {dadosTabela && (
                <table className="sr-only">
                    <caption>{titulo}</caption>
                    <thead>
                        <tr>
                            {dadosTabela.colunas.map(coluna => <th key={coluna}>{coluna}</th>)}
                        </tr>
                    </thead>
                    <tbody>
                        {dadosTabela.linhas.map((linha, indice) => (
                            <tr key={indice}>
                                {linha.map((valor, i) => <td key={i}>{valor}</td>)}
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </figure>
    );
}
