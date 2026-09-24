import { useCallback, useEffect, useState } from 'react';
import { metaReversaService, AxiosError, type ApiErrorResponse } from '../../services/Api';
import { FonteRenda, type SugestaoAporteRequestDto, type SugestaoAporteResponseDto } from '../../types/SugestaoAporte';
import { formatCurrency } from './calculadoraUtils';
import { formatPrazo } from './calculadoraValidacao';
import { PlanoDeCorte } from './PlanoDeCorte';
import { ClassificarCategoriasInline } from './ClassificarCategoriasInline';
import { Alerta, Skeleton } from '../Shared/ui';
import './SugestaoAporte.css';

interface SugestaoAporteProps {
    /** Parâmetros da meta já calculada; mudar isso refaz a sugestão. */
    request: SugestaoAporteRequestDto;
}

/**
 * Card de sugestão de comportamento financeiro. Busca o diagnóstico assim que é
 * montado (o toggle da Meta Reversa só o monta quando ligado), para que o cálculo
 * principal não pague o custo de quem nunca abre a sugestão.
 */
export function SugestaoAporte({ request }: SugestaoAporteProps) {
    const [sugestao, setSugestao] = useState<SugestaoAporteResponseDto | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [erro, setErro] = useState<string | null>(null);

    const buscar = useCallback(async () => {
        setIsLoading(true);
        setErro(null);
        try {
            setSugestao(await metaReversaService.obterSugestao(request));
        } catch (err) {
            const axiosError = err as AxiosError<ApiErrorResponse>;
            setErro(axiosError.response?.data?.message || 'Não foi possível montar a sugestão. Tente novamente.');
            setSugestao(null);
        } finally {
            setIsLoading(false);
        }
    }, [request]);

    useEffect(() => { void buscar(); }, [buscar]);

    if (isLoading) {
        return (
            <div className="sugestao-card" aria-busy="true">
                <Skeleton altura={24} largura="60%" />
                <Skeleton altura={72} />
                <Skeleton altura={120} />
            </div>
        );
    }

    if (erro) {
        return <Alerta variante="erro" rotuloAcao="Tentar novamente" onAcao={buscar}>{erro}</Alerta>;
    }

    if (!sugestao) return null;

    return (
        <div className="sugestao-card">
            <div className={`sugestao-veredito sugestao-veredito--${sugestao.cabe ? 'cabe' : 'nao-cabe'}`}>
                <span className="sugestao-veredito-titulo">
                    {sugestao.cabe ? 'Cabe no seu orçamento' : 'Não cabe no seu orçamento hoje'}
                </span>
                <p className="sugestao-veredito-texto">{sugestao.textoConsultivo}</p>
            </div>

            <dl className="sugestao-numeros">
                <div className="sugestao-numero">
                    <dt>Renda mensal</dt>
                    <dd>{formatCurrency(sugestao.rendaMensal)}</dd>
                </div>
                <div className="sugestao-numero">
                    <dt>Despesa média</dt>
                    <dd>{formatCurrency(sugestao.despesaMensalMedia)}</dd>
                </div>
                {sugestao.aportesMensaisMedios > 0 && (
                    <div className="sugestao-numero">
                        <dt>Já investe</dt>
                        <dd>{formatCurrency(sugestao.aportesMensaisMedios)}</dd>
                    </div>
                )}
                <div className="sugestao-numero">
                    <dt>Sobra livre</dt>
                    <dd>{formatCurrency(sugestao.sobraLivre)}</dd>
                </div>
                <div className="sugestao-numero sugestao-numero--destaque">
                    <dt>{sugestao.cabe ? 'Folga após o aporte' : 'Falta por mês'}</dt>
                    <dd>{formatCurrency(sugestao.cabe ? sugestao.folgaRestante : sugestao.deficit)}</dd>
                </div>
            </dl>

            <p className="sugestao-texto-apoio">
                O aporte de {formatCurrency(sugestao.aporteMensalNecessario)} consome{' '}
                {sugestao.percentualDaRenda.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}% da sua renda.{' '}
                {sugestao.fonteRenda === FonteRenda.TransacoesRealizadas
                    ? 'A renda considerada é a média das receitas lançadas — cadastre seu salário no perfil para um número mais preciso.'
                    : 'A renda considerada é o salário cadastrado no seu perfil.'}
            </p>

            {sugestao.historicoInsuficiente && (
                <Alerta variante="aviso">
                    Só encontrei {sugestao.mesesAnalisados} mês de lançamentos. As médias ainda são pouco
                    confiáveis — a sugestão melhora conforme você registra mais movimentações.
                </Alerta>
            )}

            {sugestao.reserva && !sugestao.reserva.adequada && (
                <Alerta variante="aviso">
                    Sua reserva de emergência cobre {sugestao.reserva.mesesCobertos.toLocaleString('pt-BR', { maximumFractionDigits: 1 })} mês(es)
                    de renda e faltam {formatCurrency(sugestao.reserva.valorFaltante)} para os seis meses recomendados.
                    Completá-la antes da meta protege você de precisar resgatar o investimento no pior momento.
                </Alerta>
            )}

            {sugestao.cortes.length > 0 && <PlanoDeCorte cortes={sugestao.cortes} />}

            {sugestao.naoClassificadas.length > 0 && (
                <ClassificarCategoriasInline
                    categorias={sugestao.naoClassificadas}
                    onClassificado={buscar}
                />
            )}

            {sugestao.cenarios && (
                <div className="sugestao-cenarios">
                    <h4 className="sugestao-subtitulo">O que funcionaria</h4>
                    <p className="sugestao-texto-apoio">
                        Mantendo o máximo que você sustenta hoje ({formatCurrency(sugestao.cenarios.aporteSustentavel)} por mês):
                    </p>
                    <ul className="sugestao-cenarios-lista">
                        <li>
                            {sugestao.cenarios.alvoInatingivelNoPrazoMaximo || !sugestao.cenarios.prazoMesesAlternativo
                                ? 'Mantendo o valor-alvo, a meta não é atingível nem em 50 anos com esse aporte.'
                                : `Mantendo o valor-alvo, você chega lá em ${formatPrazo(sugestao.cenarios.prazoMesesAlternativo)}.`}
                        </li>
                        {sugestao.cenarios.valorAlvoAlternativo !== undefined && (
                            <li>
                                Mantendo o prazo original, você alcança {formatCurrency(sugestao.cenarios.valorAlvoAlternativo)}.
                            </li>
                        )}
                    </ul>
                </div>
            )}

            <p className="sugestao-rodape">
                Se você tem dívida de cartão ou cheque especial, quitá-la rende mais que qualquer
                investimento simulado aqui.
                {sugestao.iaIndisponivel && ' · Texto gerado sem o agente de IA.'}
            </p>
        </div>
    );
}
