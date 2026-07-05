"""
session.py — Cliente HTTP autenticado + helpers compartilhados de acesso ao
backend .NET, reutilizados pelas tools de todos os módulos de domínio.

Arquitetura HTTP:
  Todas as chamadas são async, usando httpx.AsyncClient em vez de requests.
  Isso evita que chamadas HTTP bloqueiem o event loop durante graph.ainvoke().

  buscar_todas_transacoes usa asyncio.gather para disparar as requisições de
  transações de todas as contas em paralelo — para um usuário com N contas,
  o tempo de resposta cai de N × latência para 1 × latência (maior conta).
"""
import asyncio
import logging
from datetime import datetime, timedelta, timezone

import httpx

from src.Infra.Config.settings import get_settings
from .errors import ApiOffline, SessionExpired, ApiStatus
from . import routes

_logger = logging.getLogger("myfinance.agent")
_API_BASE_URL = get_settings().api_url


class ApiSession:
    """
    Encapsula o AsyncClient com o JWT baked-in e os helpers de acesso ao
    backend .NET compartilhados entre os módulos de tools.

    Uma instância é criada por chamada a make_api_tools (ver registry.py) e
    reutilizada por todas as tools da mesma requisição — o LLM nunca vê o
    token, e headers/pool de conexão são compartilhados entre as chamadas.
    O cliente é finalizado pelo GC quando o grafo encerra.
    """

    def __init__(self, jwt_token: str) -> None:
        self._client = httpx.AsyncClient(
            headers={"Authorization": f"Bearer {jwt_token}"},
            timeout=10.0,
        )

    # ── Helpers HTTP ──────────────────────────────────────────────────────────

    async def get_raw(self, path: str) -> httpx.Response:
        """GET cru, sem tradução de status — usado quando o caller precisa
        inspecionar múltiplas respostas de uma vez (ex: asyncio.gather)."""
        return await self._client.get(f"{_API_BASE_URL}{path}")

    async def get_json(self, path: str):
        """GET no .NET; levanta ApiOffline/SessionExpired/ApiStatus e devolve o JSON."""
        try:
            r = await self._client.get(f"{_API_BASE_URL}{path}")
        except httpx.RequestError as e:
            raise ApiOffline(str(e)) from e
        if r.status_code == 401:
            raise SessionExpired()
        if r.status_code != 200:
            raise ApiStatus(r.status_code, r.text)
        return r.json()

    async def post_json(self, path: str, payload: dict):
        """POST no .NET; aceita 2xx, levanta erro tipado caso contrário. Devolve o JSON (ou None)."""
        try:
            r = await self._client.post(f"{_API_BASE_URL}{path}", json=payload)
        except httpx.RequestError as e:
            raise ApiOffline(str(e)) from e
        if r.status_code == 401:
            raise SessionExpired()
        if r.status_code not in (200, 201):
            raise ApiStatus(r.status_code, r.text)
        return r.json() if r.content else None

    # ── Busca concorrente de transações ─────────────────────────────────────

    async def _buscar_transacoes_conta(
        self, account_id: str, dt_inicio: datetime, dt_fim: datetime,
    ) -> list:
        """
        Busca as transações de UMA conta no período e filtra pelo intervalo.
        Projetada para ser executada em paralelo via asyncio.gather — cada
        conta dispara sua própria requisição sem esperar as demais.
        Falhas individuais retornam lista vazia, não propagam exceção.
        """
        try:
            path = routes.ACCOUNT_TRANSACTIONS.format(account_id=account_id)
            r = await self._client.get(f"{_API_BASE_URL}{path}")
            if r.status_code != 200:
                return []
            resultado = []
            for t in (r.json() or []):
                date_str = t.get("date", "")
                try:
                    tx_date = datetime.fromisoformat(date_str.replace("Z", "+00:00"))
                    if tx_date.tzinfo is None:
                        tx_date = tx_date.replace(tzinfo=timezone.utc)
                    if dt_inicio <= tx_date <= dt_fim:
                        resultado.append(t)
                except Exception:
                    pass
            return resultado
        except Exception:
            return []

    async def buscar_todas_transacoes(self, dt_inicio: datetime, dt_fim: datetime) -> list | str:
        """
        Busca transações de TODAS as contas do usuário em paralelo.

        Fluxo:
          1. Busca lista de contas (1 requisição sequencial obrigatória).
          2. Dispara 1 requisição por conta simultaneamente via asyncio.gather.
          3. Agrega os resultados, ignorando contas que falharam.

        Ganho: para N contas, o tempo de resposta é 1 × latência (maior conta)
        em vez de N × latência (sequencial com requests síncrono).
        """
        contas = await self.get_json(routes.ACCOUNTS) or []
        if not contas:
            return "O usuário não possui contas cadastradas."

        ids = [c["id"] for c in contas if c.get("id")]
        if not ids:
            return "Nenhuma conta com ID válido encontrada."

        grupos = await asyncio.gather(
            *[self._buscar_transacoes_conta(aid, dt_inicio, dt_fim) for aid in ids],
            return_exceptions=True,
        )

        todas = []
        falhas = 0
        for grupo in grupos:
            if isinstance(grupo, list):
                todas.extend(grupo)
            else:
                falhas += 1

        _logger.info(
            "🔎 [API] %d transação(ões) agregadas de %d conta(s)%s",
            len(todas), len(ids),
            f" — {falhas} conta(s) falharam" if falhas else "",
        )
        return todas


def resolver_periodo(data_inicio: str, data_fim: str, ultimos_dias: int = 30) -> tuple:
    """
    Converte parâmetros do usuário em um intervalo de datas concreto.

    Prioridade:
      1. data_inicio (YYYY-MM-DD) fornecida → usa data_inicio + (data_fim ou agora).
      2. Nenhuma data → janela relativa de ultimos_dias a partir de agora.

    Returns:
        (dt_inicio, dt_fim, label, dias_periodo)
    """
    utc = timezone.utc
    agora = datetime.now(utc)

    if data_inicio.strip():
        try:
            dt_i = datetime.strptime(data_inicio.strip(), "%Y-%m-%d").replace(tzinfo=utc)
        except ValueError:
            dt_i = agora - timedelta(days=ultimos_dias)

        if data_fim.strip():
            try:
                dt_f = datetime.strptime(data_fim.strip(), "%Y-%m-%d").replace(
                    hour=23, minute=59, second=59, tzinfo=utc
                )
            except ValueError:
                dt_f = agora
        else:
            dt_f = agora

        label = f"{dt_i.strftime('%d/%m/%Y')} a {dt_f.strftime('%d/%m/%Y')}"
    else:
        dt_f = agora
        dt_i = agora - timedelta(days=ultimos_dias)
        label = f"últimos {ultimos_dias} dias"

    dias_periodo = max(1, (dt_f - dt_i).days + 1)
    return dt_i, dt_f, label, dias_periodo
