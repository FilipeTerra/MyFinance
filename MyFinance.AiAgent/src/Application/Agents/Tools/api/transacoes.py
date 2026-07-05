"""transacoes.py — Ferramentas de consulta e análise de transações."""
import logging
from datetime import datetime, timedelta, timezone

from langchain_core.tools import tool
from pydantic import BaseModel, Field

from .errors import handle_api_errors
from .fluxo import classificar_fluxo
from .session import ApiSession, resolver_periodo

_logger = logging.getLogger("myfinance.agent")

# strftime('%B') devolve o mês no locale do sistema (geralmente inglês).
# Mapa fixo garante saída sempre em português, independente do ambiente.
_MESES_PT = {
    1: "Janeiro", 2: "Fevereiro", 3: "Março", 4: "Abril",
    5: "Maio", 6: "Junho", 7: "Julho", 8: "Agosto",
    9: "Setembro", 10: "Outubro", 11: "Novembro", 12: "Dezembro",
}


class SimularEstresseOrcamentoInput(BaseModel):
    descricao_nova_despesa: str = Field(
        ...,
        description="O que o usuário deseja assumir. Ex: 'Financiamento do carro', 'Aluguel mais caro'."
    )
    valor_mensal: float = Field(
        ...,
        description="O valor mensal da nova despesa em reais. Ex: 1200.00"
    )
    tipo_despesa: str = Field(
        ...,
        description="Classifique a despesa em: 'essencial' (moradia, transporte, saúde), 'estilo_de_vida' (lazer, assinaturas) ou 'divida'."
    )


def build(session: ApiSession) -> list:
    @tool
    @handle_api_errors()
    async def consultar_transacoes_recentes(
        limite: int = 15,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta APENAS para listar transações individuais quando o usuário quiser
        ver o extrato ou histórico de movimentações específicas (ex: 'mostre minhas últimas transações',
        'o que comprei em maio', 'qual foi minha última compra'). NÃO use para análise de gastos
        por categoria ou resumo — use analisar_gastos_por_categoria e calcular_resumo_financeiro.
        Parâmetros: limite (padrão 15); data_inicio e data_fim em YYYY-MM-DD para períodos específicos
        (ex: maio de 2026 → data_inicio='2026-05-01', data_fim='2026-05-31')."""
        _TIPO_EMOJI = {1: "💰", 2: "💸", 3: "📈"}

        def _parse_date(t: dict) -> datetime:
            try:
                return datetime.fromisoformat(t.get("date", "").replace("Z", "+00:00"))
            except Exception:
                return datetime.min.replace(tzinfo=timezone.utc)

        dt_i, dt_f, label, _ = resolver_periodo(data_inicio, data_fim, 90)
        transacoes = await session.buscar_todas_transacoes(dt_i, dt_f)
        if isinstance(transacoes, str):
            return transacoes
        if not transacoes:
            return f"Nenhuma transação encontrada no período {label}."

        transacoes.sort(key=_parse_date, reverse=True)
        total = len(transacoes)
        exibidas = transacoes[:limite]

        linhas = [f"📋 {len(exibidas)} transações — {label}\n"]
        for t in exibidas:
            tx_type = t.get("type", 0)
            amount = t.get("amount", 0.0)
            emoji = _TIPO_EMOJI.get(tx_type, "•")
            sinal = "+" if amount > 0 else "-" if amount < 0 else ""
            try:
                data_fmt = _parse_date(t).strftime("%d/%m")
            except Exception:
                data_fmt = "??"
            descricao = (t.get("description") or "Sem descrição")[:40]
            categoria = t.get("categoryName") or t.get("category") or "Sem categoria"
            linhas.append(
                f"  {emoji} {data_fmt} | {descricao} | {categoria} | {sinal}R$ {abs(amount):,.2f}"
            )

        if total > limite:
            linhas.append(f"\n  ℹ️ Exibindo {limite} de {total} transações no período.")

        _logger.info(
            "📋 [TOOL:extrato] %d/%d transação(ões) exibidas | período: %s",
            len(exibidas), total, label,
        )
        return "\n".join(linhas)

    @tool
    @handle_api_errors()
    async def analisar_gastos_por_categoria(
        ultimos_dias: int = 30,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta para analisar onde o usuário está gastando mais dinheiro,
        identificar padrões de consumo, responder 'onde gasto mais?', 'como melhorar meus gastos?'
        ou qualquer pergunta sobre categorias de despesas. Agrupa despesas por categoria e mostra
        totais e percentuais. Use data_inicio e data_fim (YYYY-MM-DD) para períodos específicos
        (ex: maio de 2026 → data_inicio='2026-05-01', data_fim='2026-05-31'); omita para usar
        ultimos_dias (padrão 30)."""
        dt_i, dt_f, label, _ = resolver_periodo(data_inicio, data_fim, ultimos_dias)
        transacoes = await session.buscar_todas_transacoes(dt_i, dt_f)
        if isinstance(transacoes, str):
            return transacoes

        gastos: dict[str, float] = {}
        total_despesas = 0.0

        for t in transacoes:
            tx_type = t.get("type", 0)
            amount = t.get("amount", 0)
            if amount >= 0 or tx_type == 3:
                continue
            categoria = (
                t.get("categoryName")
                or t.get("category")
                or "Sem categoria"
            )
            valor = abs(amount)
            gastos[categoria] = gastos.get(categoria, 0.0) + valor
            total_despesas += valor

        if not gastos:
            return f"Nenhuma despesa encontrada no período {label}."

        linhas = [f"📊 Gastos por categoria — {label}\n"]
        for cat, total in sorted(gastos.items(), key=lambda x: x[1], reverse=True):
            pct = (total / total_despesas * 100) if total_despesas > 0 else 0
            linhas.append(f"  • {cat}: R$ {total:,.2f} ({pct:.1f}%)")
        linhas.append(f"\n  💸 Total gasto: R$ {total_despesas:,.2f}")
        _logger.info(
            "📊 [TOOL:gastos] %d categoria(s) | total R$ %.2f | período: %s",
            len(gastos), total_despesas, label,
        )
        return "\n".join(linhas)

    @tool
    @handle_api_errors()
    async def relatorio_mensal_por_categoria(
        filtro_categoria: str,
        ultimos_meses: int = 3,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta quando o usuário quiser um relatório detalhado de gastos em uma
        categoria ou tipo de gasto específico (ex: 'transporte', 'uber', 'alimentação', 'lazer')
        quebrado mês a mês. Exemplos: 'quanto gastei com uber nos últimos 3 meses?',
        'relatório de alimentação em maio de 2026', 'quanto gastei com transporte por mês?'.
        Parâmetros: filtro_categoria (palavra-chave, ex: 'uber', 'alimentação'), ultimos_meses
        (padrão 3); use data_inicio e data_fim (YYYY-MM-DD) para períodos específicos."""
        dt_i, dt_f, label, _ = resolver_periodo(data_inicio, data_fim, ultimos_meses * 31)
        if not data_inicio.strip():
            label = f"últimos {ultimos_meses} meses"
        transacoes = await session.buscar_todas_transacoes(dt_i, dt_f)
        if isinstance(transacoes, str):
            return transacoes

        filtro = filtro_categoria.lower().strip()
        meses: dict[str, dict] = {}

        for t in transacoes:
            tx_type = t.get("type", 0)
            amount = t.get("amount", 0)
            if amount >= 0 or tx_type == 3:
                continue

            categoria = (t.get("categoryName") or t.get("category") or "").lower()
            descricao = (t.get("description") or "").lower()
            if filtro not in categoria and filtro not in descricao:
                continue

            date_str = t.get("date", "")
            try:
                tx_date = datetime.fromisoformat(date_str.replace("Z", "+00:00"))
                chave_mes = tx_date.strftime("%Y-%m")
                label_mes = f"{_MESES_PT[tx_date.month]}/{tx_date.year}"
                data_fmt = tx_date.strftime("%d/%m")
            except Exception:
                # Sem data válida: não reutiliza tx_date de iteração anterior
                chave_mes = "desconhecido"
                label_mes = "Data desconhecida"
                data_fmt = "??"

            if chave_mes not in meses:
                meses[chave_mes] = {"label": label_mes, "total": 0.0, "transacoes": []}
            valor = abs(amount)
            meses[chave_mes]["total"] += valor
            meses[chave_mes]["transacoes"].append({
                "data": data_fmt,
                "descricao": t.get("description", "Sem descrição"),
                "categoria": t.get("categoryName") or t.get("category") or "Sem categoria",
                "valor": valor,
            })

        if not meses:
            return f"Nenhuma despesa encontrada com o filtro '{filtro_categoria}' no período {label}."

        _MAX_TX_POR_MES = 5

        total_geral = sum(m["total"] for m in meses.values())
        linhas = [f"📊 Relatório de '{filtro_categoria}' — {label}\n"]

        for chave in sorted(meses.keys(), reverse=True):
            mes = meses[chave]
            linhas.append(f"\n📅 {mes['label']} — R$ {mes['total']:,.2f}")
            ordenadas = sorted(mes["transacoes"], key=lambda x: x["valor"], reverse=True)
            for tx in ordenadas[:_MAX_TX_POR_MES]:
                linhas.append(
                    f"  • {tx['data']} | {tx['descricao'][:40]} | {tx['categoria']} | R$ {tx['valor']:,.2f}"
                )
            resto = ordenadas[_MAX_TX_POR_MES:]
            if resto:
                valor_resto = sum(r["valor"] for r in resto)
                linhas.append(f"  ... e mais {len(resto)} transação(ões) — R$ {valor_resto:,.2f}")

        linhas.append(f"\n💸 Total no período: R$ {total_geral:,.2f}")
        linhas.append(f"📈 Média mensal: R$ {total_geral / len(meses):,.2f}")
        _logger.info(
            "📊 [TOOL:relatorio] filtro='%s' | %d mês(es) | total R$ %.2f",
            filtro_categoria, len(meses), total_geral,
        )
        return "\n".join(linhas)

    @tool
    @handle_api_errors()
    async def calcular_resumo_financeiro(
        ultimos_dias: int = 30,
        data_inicio: str = "",
        data_fim: str = "",
    ) -> str:
        """Use esta ferramenta para obter um raio-x completo das finanças do usuário: receitas,
        despesas, investimentos, saldo líquido, gasto médio diário, categoria com maior gasto,
        maior despesa única e taxa de poupança. Use quando o usuário perguntar sobre saúde financeira,
        balanço do mês, situação geral, quanto está poupando ou investindo.
        Use data_inicio e data_fim (YYYY-MM-DD) para períodos específicos
        (ex: maio de 2026 → data_inicio='2026-05-01', data_fim='2026-05-31'); omita para usar
        ultimos_dias (padrão 30)."""
        dt_i, dt_f, label, dias_periodo = resolver_periodo(data_inicio, data_fim, ultimos_dias)
        transacoes = await session.buscar_todas_transacoes(dt_i, dt_f)
        if isinstance(transacoes, str):
            return transacoes

        if not transacoes:
            return f"Nenhuma transação encontrada no período {label}."

        total_receitas = 0.0
        total_despesas = 0.0
        total_investimentos = 0.0
        gastos_categoria: dict[str, float] = {}
        maior_despesa_valor = 0.0
        maior_despesa_desc = "N/A"

        for t in transacoes:
            tx_type = t.get("type", 0)
            amount = t.get("amount", 0)
            categoria = t.get("categoryName") or t.get("category") or "Sem categoria"
            descricao = (t.get("description") or "Sem descrição")[:50]

            if tx_type == 3:
                total_investimentos += abs(amount)
            elif tx_type == 2 or (tx_type not in (1, 3) and amount < 0):
                valor = abs(amount)
                total_despesas += valor
                gastos_categoria[categoria] = gastos_categoria.get(categoria, 0.0) + valor
                if valor > maior_despesa_valor:
                    maior_despesa_valor = valor
                    maior_despesa_desc = descricao
            elif tx_type == 1 or (tx_type not in (2, 3) and amount > 0):
                total_receitas += amount

        # Saldo do período = receitas − despesas. Aportes NÃO entram: o
        # dinheiro aportado em meta continua sendo patrimônio do usuário
        # (apenas mudou de lugar). Sem esta separação, um mês saudável com
        # aporte alto aparecia como "negativo" — dado incorreto ao usuário.
        saldo_periodo = total_receitas - total_despesas
        sobra_apos_aportes = saldo_periodo - total_investimentos
        gasto_medio_diario = total_despesas / dias_periodo
        taxa_poupanca = (total_investimentos / total_receitas * 100) if total_receitas > 0 else 0.0
        situacao = "✅ positivo" if saldo_periodo >= 0 else "❌ negativo"

        if gastos_categoria:
            cat_vila = max(gastos_categoria, key=lambda k: gastos_categoria[k])
            cat_vila_valor = gastos_categoria[cat_vila]
        else:
            cat_vila, cat_vila_valor = "N/A", 0.0

        # Texto em TÓPICOS curtos agrupados por bloco — mais intuitivo para
        # o usuário visualizar um resumo completo do que um parágrafo único.
        # Sem "##"/"###" (headers grandes): grupos em **negrito** e bullets
        # simples mantêm a hierarquia visual leve, e o CSS do frontend não
        # duplica espaçamento entre itens (bug de white-space já corrigido).
        linhas = [
            f"📊 Resumo financeiro — {label}",
            "",
            "**Fluxo de caixa**",
            f"• Receitas: R$ {total_receitas:,.2f}",
            f"• Despesas: R$ {total_despesas:,.2f}",
            f"• Saldo do período: R$ {saldo_periodo:,.2f} ({situacao})",
        ]
        if total_investimentos > 0:
            linhas.append(f"• Aportes em metas/investimentos: R$ {total_investimentos:,.2f}")
            linhas.append(f"• Sobra em conta após aportes: R$ {sobra_apos_aportes:,.2f}")

        linhas += [
            "",
            "**Indicadores**",
            f"• Gasto médio diário: R$ {gasto_medio_diario:,.2f}",
        ]
        if total_investimentos > 0:
            linhas.append(f"• Taxa de poupança (investido/receita): {taxa_poupanca:.1f}%")

        if cat_vila != "N/A":
            linhas += [
                "",
                "**Destaques**",
                f"• Categoria que mais pesou: {cat_vila} (R$ {cat_vila_valor:,.2f})",
                f"• Maior despesa única: {maior_despesa_desc} (R$ {maior_despesa_valor:,.2f})",
            ]

        _logger.info(
            "📋 [TOOL:resumo] receitas=R$ %.2f | despesas=R$ %.2f | invest=R$ %.2f | período: %s",
            total_receitas, total_despesas, total_investimentos, label,
        )
        return "\n".join(linhas)

    @tool(args_schema=SimularEstresseOrcamentoInput)
    @handle_api_errors(as_dict=True)
    async def simular_impacto_nova_despesa(descricao_nova_despesa: str, valor_mensal: float, tipo_despesa: str) -> dict:
        """
        Use esta ferramenta quando o usuário perguntar se 'dá conta' de assumir uma nova conta,
        se o orçamento aguenta uma nova parcela, ou simular o impacto de um novo gasto fixo.
        """
        dt_fim = datetime.now(timezone.utc)
        dt_inicio = dt_fim - timedelta(days=30)

        transacoes = await session.buscar_todas_transacoes(dt_inicio, dt_fim)
        if isinstance(transacoes, str):
            return {"erro": transacoes}

        # Classificador único: aporte em meta NÃO é despesa, mas é dinheiro
        # comprometido — entra no cálculo de margem como item separado.
        total_receitas, despesas_atuais, aportes_metas = classificar_fluxo(transacoes)
        compromissos_atuais = despesas_atuais + aportes_metas

        # Matemática determinística do cenário
        novo_total_compromissos = compromissos_atuais + valor_mensal
        saldo_livre_atual = total_receitas - compromissos_atuais
        novo_saldo_livre = total_receitas - novo_total_compromissos

        comprometimento_atual_pct = (compromissos_atuais / total_receitas * 100) if total_receitas > 0 else 0
        novo_comprometimento_pct = (novo_total_compromissos / total_receitas * 100) if total_receitas > 0 else 0

        # Classificação de Risco
        status_risco = "SEGURO"
        if novo_saldo_livre < 0:
            status_risco = "CRÍTICO - Orçamento ficará negativo"
        elif novo_comprometimento_pct > 80:
            status_risco = "ALTO RISCO - Restará pouca margem de segurança"

        _logger.info(
            "🧪 [TOOL:estresse] '%s' R$ %.2f/mês → %s (comprometimento %.1f%% → %.1f%%)",
            descricao_nova_despesa, valor_mensal, status_risco.split(" -")[0],
            comprometimento_atual_pct, novo_comprometimento_pct,
        )
        return {
            "analise": f"Impacto de assumir: {descricao_nova_despesa}",
            "cenario_atual": {
                "receita_mensal": round(total_receitas, 2),
                "despesa_mensal": round(despesas_atuais, 2),
                "aportes_em_metas": round(aportes_metas, 2),
                "saldo_livre": round(saldo_livre_atual, 2),
                "comprometimento_renda_pct": round(comprometimento_atual_pct, 1)
            },
            "cenario_simulado": {
                "nova_despesa": round(valor_mensal, 2),
                "novo_total_compromissos": round(novo_total_compromissos, 2),
                "novo_saldo_livre": round(novo_saldo_livre, 2),
                "novo_comprometimento_renda_pct": round(novo_comprometimento_pct, 1)
            },
            "status_de_risco": status_risco
        }

    return [
        consultar_transacoes_recentes,
        analisar_gastos_por_categoria,
        relatorio_mensal_por_categoria,
        calcular_resumo_financeiro,
        simular_impacto_nova_despesa,
    ]
