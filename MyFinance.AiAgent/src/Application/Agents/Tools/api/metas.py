"""metas.py — Ferramentas de consulta, criação, aporte e simulação de metas financeiras."""
import logging
import math
from datetime import datetime, timedelta, timezone
from typing import Optional

from langchain_core.tools import tool
from pydantic import BaseModel, Field

from .errors import handle_api_errors
from .fluxo import classificar_fluxo, listar_maiores_despesas
from .session import ApiSession
from . import routes

_logger = logging.getLogger("myfinance.agent")


class SimularMetaIdealInput(BaseModel):
    objetivo_principal: str = Field(
        ...,
        description="O objetivo da meta. Ex: 'Reserva de Emergência', 'Entrada do Apartamento'."
    )
    valor_alvo_estimado: Optional[float] = Field(
        None,
        description="Opcional. O valor financeiro que o utilizador deseja atingir, se ele tiver mencionado. Ex: 50000 para um apartamento."
    )
    prazo_meses_desejado: Optional[int] = Field(
        None,
        description="Opcional. Em quantos meses o utilizador quer atingir a meta, se ele tiver mencionado."
    )


def build(session: ApiSession) -> list:
    @tool(extras={"retorna_dinheiro": True})
    @handle_api_errors()
    async def consultar_metas_financeiras() -> str:
        """Use esta ferramenta para verificar as metas financeiras do usuário
        (ex: comprar carro, fundo de emergência), ver o progresso, valores alvo e se a meta foi concluída."""
        metas = await session.get_json(routes.FINANCIAL_GOALS) or []
        if not metas:
            return "O usuário ainda não possui metas financeiras cadastradas."

        # Saída compacta: nome, progresso, prazo, status e id (necessário
        # para realizar_aporte_meta). JSON cru inflaria o contexto.
        linhas = [f"🎯 {len(metas)} meta(s):"]
        for m in metas:
            atual = m.get("currentAmount", 0.0)
            alvo = m.get("targetAmount", 0.0)
            pct = m.get("progressPercentage") or (atual / alvo * 100 if alvo else 0.0)
            status = "✅ concluída" if m.get("isCompleted") else "em andamento"
            prazo = (m.get("deadline") or "")[:10]
            linhas.append(
                f"  • {m.get('name', '?')}: R$ {atual:,.2f} / R$ {alvo:,.2f} "
                f"({pct:.1f}%) | prazo {prazo} | {status} | id={m.get('id', '?')}"
            )
        _logger.info("🎯 [TOOL:metas] %d meta(s) retornadas", len(metas))
        return "\n".join(linhas)

    @tool
    @handle_api_errors()
    async def criar_meta_financeira(nome: str, valor_alvo: float, data_limite: str) -> str:
        """Use esta ferramenta para criar uma nova meta financeira para o usuário APENAS quando
        ele pedir explicitamente. Exemplos: 'Quero criar uma meta para comprar um carro de 50000
        até dezembro', 'Cria uma meta de viagem de R$5000 para junho de 2026'.
        Recebe o nome da meta, o valor alvo e a data limite no formato 'YYYY-MM-DD'."""
        payload = {
            "name": nome,
            "targetAmount": valor_alvo,
            "deadline": f"{data_limite}T00:00:00",
        }
        _logger.info(
            "✍️  [TOOL:criar_meta] Solicitação: nome='%s' | alvo=R$ %.2f | prazo=%s",
            nome, valor_alvo, data_limite,
        )
        await session.post_json(routes.FINANCIAL_GOALS, payload)
        _logger.info("✅ [TOOL:criar_meta] Meta '%s' criada com sucesso", nome)
        return (
            f"✅ Meta criada com sucesso!\n"
            f"  • Nome: {nome}\n"
            f"  • Valor alvo: R$ {valor_alvo:,.2f}\n"
            f"  • Prazo: {data_limite}"
        )

    @tool
    @handle_api_errors()
    async def realizar_aporte_meta(valor: float, goal_id: str, account_id: str) -> str:
        """Use esta ferramenta para investir ou guardar dinheiro em uma meta financeira específica.
        Recebe o valor, o ID da meta e o ID da conta de origem. Retorna sucesso ou erro."""
        _logger.info(
            "✍️  [TOOL:aporte] Solicitação: R$ %.2f | meta=%s | conta=%s",
            valor, goal_id[:8], account_id[:8],
        )
        categorias = await session.get_json(routes.CATEGORIES) or []
        if not categorias:
            _logger.warning("⚠️  [TOOL:aporte] Nenhuma categoria disponível — aporte abortado")
            return "Erro: Nenhuma categoria encontrada. Crie pelo menos uma categoria antes de realizar um aporte."

        payload = {
            "amount": valor,
            "type": 3,
            "accountId": account_id,
            "financialGoalId": goal_id,
            "description": "Aporte na meta",
            "date": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S"),
            "categoryId": categorias[0]["id"],
        }
        await session.post_json(routes.TRANSACTIONS, payload)
        _logger.info("✅ [TOOL:aporte] R$ %.2f aportados na meta %s", valor, goal_id[:8])
        return f"✅ Aporte de R$ {valor:,.2f} realizado com sucesso na meta!"

    @tool(args_schema=SimularMetaIdealInput)
    @handle_api_errors(as_dict=True)
    async def simular_meta_ideal(objetivo_principal: str, valor_alvo_estimado: float = None, prazo_meses_desejado: int = None) -> dict:
        """
        Use esta ferramenta ANTES de criar uma meta quando o utilizador pedir ajuda para se organizar.
        Ela analisa o fluxo de caixa e devolve uma proposta matemática viável ou oportunidades de corte.
        """
        dt_fim = datetime.now(timezone.utc)
        dt_inicio = dt_fim - timedelta(days=30)

        transacoes = await session.buscar_todas_transacoes(dt_inicio, dt_fim)
        if isinstance(transacoes, str):
            return {"erro": transacoes}

        # Classificador único: aportes já feitos são compromisso (reduzem a
        # sobra disponível para uma NOVA meta), mas não são "despesa".
        total_receitas, despesas_atuais, aportes_metas = classificar_fluxo(transacoes)
        saldo_livre = total_receitas - despesas_atuais - aportes_metas

        if saldo_livre <= 0:
            # Mantém a lógica de corte de despesas semântica que já fizemos
            _logger.info(
                "🧮 [TOOL:meta_ideal] '%s' → orçamento negativo (rombo R$ %.2f) — sugerindo cortes",
                objetivo_principal, abs(saldo_livre),
            )
            maiores_despesas = listar_maiores_despesas(transacoes, limite=8)
            return {
                "analise": "O orçamento atual está negativo ou zerado. Impossível criar meta.",
                "cenario_atual": {
                    "receita_mensal": round(total_receitas, 2),
                    "despesa_mensal": round(despesas_atuais, 2),
                    "rombo_mensal": round(abs(saldo_livre), 2)
                },
                "maiores_despesas_do_mes": maiores_despesas,
                "instrucao_ao_agente": (
                    "NÃO crie a meta. Analise a lista 'maiores_despesas_do_mes' usando a sua capacidade "
                    "de julgamento semântico. Identifique quais itens NÃO SÃO de sobrevivência básica. "
                    "Apresente-os ao utilizador e sugira um plano de corte focado neles para sair do vermelho."
                )
            }

        # Capacidade de poupança conservadora (poupando toda a sobra ou metade dela)
        capacidade_maxima = saldo_livre
        aporte_sugerido = round(saldo_livre * 0.5, 2)

        # === ÁRVORE DE DECISÃO DINÂMICA ===

        # Cenário 1: O utilizador sabe o valor E o prazo (Ex: "Quero 100k para um apartamento em 3 anos")
        if valor_alvo_estimado and prazo_meses_desejado:
            aporte_necessario = round(valor_alvo_estimado / prazo_meses_desejado, 2)

            if aporte_necessario > capacidade_maxima:
                status = "INVIÁVEL NO ORÇAMENTO ATUAL"
                instrucao = f"O aporte exigido (R$ {aporte_necessario}) é maior que a sobra mensal do utilizador (R$ {saldo_livre}). Sugira que ele estique o prazo ou que corte agressivamente as despesas."
            else:
                status = "VIÁVEL"
                instrucao = "O valor cabe no orçamento. Valide se ele quer criar a meta com este prazo e valor."
                aporte_sugerido = aporte_necessario

            valor_alvo = valor_alvo_estimado
            prazo_meses = prazo_meses_desejado

        # Cenário 2: O utilizador sabe o valor, mas não sabe quando consegue (Ex: "Preciso de 50k para um carro")
        elif valor_alvo_estimado:
            valor_alvo = valor_alvo_estimado
            # Calculamos o prazo com base no aporte sugerido (50% da sobra)
            prazo_meses = math.ceil(valor_alvo / aporte_sugerido) if aporte_sugerido > 0 else 0
            status = "CÁLCULO DE PRAZO"
            instrucao = f"Com base na sobra de caixa, calculei que levará {prazo_meses} meses poupando R$ {aporte_sugerido}/mês. Pergunte se o prazo o agrada."

        # Cenário 3: O utilizador sabe o prazo, mas quer saber quanto junta (Ex: "Vou casar daqui a 12 meses")
        elif prazo_meses_desejado:
            prazo_meses = prazo_meses_desejado
            valor_alvo = round(aporte_sugerido * prazo_meses, 2)
            status = "CÁLCULO DE POTENCIAL"
            instrucao = f"Poupando R$ {aporte_sugerido} por mês de forma segura, ele pode acumular R$ {valor_alvo} no prazo desejado. Apresente essa estimativa."

        # Cenário 4: O utilizador está perdido (A lógica clássica)
        else:
            if "reserva" in objetivo_principal.lower() or "emergência" in objetivo_principal.lower():
                valor_alvo = round(despesas_atuais * 6, 2)
            else:
                valor_alvo = round(aporte_sugerido * 12, 2)
            prazo_meses = math.ceil(valor_alvo / aporte_sugerido) if aporte_sugerido > 0 else 0
            status = "SUGESTÃO DIRECIONADA"
            instrucao = "O utilizador estava indeciso. Apresente o valor alvo sugerido e o prazo como um plano de ação ideal e pergunte se ele concorda."

        _logger.info(
            "🧮 [TOOL:meta_ideal] '%s' → %s | alvo=R$ %.2f | aporte=R$ %.2f/mês | %d meses",
            objetivo_principal, status, valor_alvo, aporte_sugerido, prazo_meses,
        )
        return {
            "objetivo": objetivo_principal,
            "status_simulacao": status,
            "cenario_atual": {
                "sobra_mensal": round(saldo_livre, 2),
                "aportes_ja_comprometidos": round(aportes_metas, 2)
            },
            "proposta_meta": {
                "valor_total_alvo": valor_alvo,
                "aporte_mensal_sugerido": aporte_sugerido,
                "prazo_estimado_meses": prazo_meses
            },
            "instrucao_ao_agente": instrucao
        }

    return [consultar_metas_financeiras, criar_meta_financeira, realizar_aporte_meta, simular_meta_ideal]
