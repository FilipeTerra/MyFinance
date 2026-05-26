from __future__ import annotations

import json
import logging
import re
from typing import List, Optional

from langchain_core.messages import HumanMessage, SystemMessage
from langchain_core.output_parsers import JsonOutputParser
from langchain_ollama import ChatOllama

from src.Domain.models import ExtractedTransaction, TransactionList
from src.Infra.Llm.ollama_provider import get_model, get_ollama_config

_logger = logging.getLogger("myfinance.agent")

_SYSTEM_PROMPT = """Você é um extrator de dados financeiros especializado em extratos e faturas bancárias brasileiras.
Leia o texto bruto de extratos ou faturas (CSV, PDF) e extraia as transações no formato JSON exigido.
Ignore cabeçalhos, rodapés e linhas irrelevantes.

REGRAS PARA DETERMINAR O TIPO (prioridade decrescente):

1. Se o campo "Tipo" ou a descrição contiver palavras como "Compra à vista", "Compra parcelada", \
"Parcela", "Débito", "Tarifa", "Anuidade", "Juros" → tipo SEMPRE = "despesa", \
independentemente do sinal do valor.

2. Se contiver "Crédito", "Estorno", "Reembolso", "Cashback", "PIX recebido", \
"TED recebida", "DOC recebido", "Rendimento", "Pagamento recebido" → tipo = "receita".

3. Em FATURAS DE CARTÃO DE CRÉDITO: todas as compras são "despesa". \
Apenas estornos e pagamentos da fatura são "receita".

4. Se nenhuma das regras acima se aplicar: use o sinal do valor como desempate \
(negativo → "despesa", positivo → "receita").

REGRAS PARA OS CAMPOS:
- data: converter para o formato YYYY-MM-DD obrigatoriamente.
- valor: sempre positivo (absoluto), sem "R$", sem pontos de milhar, com ponto decimal.
- categoria: preencher se o documento informar explicitamente (ex: "TRANSPORTE", "DROGARIA"). Se não houver, retornar null.

Retorne SOMENTE um JSON válido, sem texto extra, no formato:
{"transacoes": [{"data": "YYYY-MM-DD", "descricao": "...", "valor": 0.0, "tipo": "receita|despesa", "categoria": "...|null"}]}"""


class SemanticExtractor:
    """
    Extrator semântico universal de transações financeiras.

    Recebe texto bruto (CSV, PDF) e devolve List[ExtractedTransaction].
    Divide o texto em chunks de `lines_per_chunk` linhas para não estourar
    o contexto do modelo, concatenando os resultados ao final.
    """

    def __init__(
        self,
        model: Optional[str] = None,
        lines_per_chunk: int = 20,
    ) -> None:
        self.lines_per_chunk = lines_per_chunk

        config = get_ollama_config()
        model_name = model or get_model("extractor")

        _logger.info("🧠 [EXTRATOR] Modelo: %s | chunk: %d linhas", model_name, lines_per_chunk)

        self._llm = ChatOllama(
            model=model_name,
            base_url=config["base_url"],
            client_kwargs=config["client_kwargs"],
            format="json",
            temperature=0,
        )

        # with_structured_output é preferível; JsonOutputParser serve de fallback
        # caso o proxy ou modelo não suporte a instrução de schema.
        self._structured_llm = None
        try:
            self._structured_llm = self._llm.with_structured_output(TransactionList)
            _logger.info("✅ [EXTRATOR] Modo structured_output ativado.")
        except Exception as exc:
            _logger.warning(
                "⚠️  [EXTRATOR] with_structured_output indisponível (%s). "
                "Usando JsonOutputParser como fallback.",
                exc,
            )

        self._json_parser = JsonOutputParser(pydantic_object=TransactionList)

    # ── API pública ────────────────────────────────────────────────────────────

    def extract_from_text(self, raw_text: str) -> List[ExtractedTransaction]:
        """Divide o texto em chunks, extrai cada um e devolve a lista unificada."""
        chunks = self._chunk_text(raw_text)
        total = len(chunks)
        _logger.info("📄 [EXTRATOR] %d chunk(s) para processar.", total)

        all_transactions: List[ExtractedTransaction] = []
        for idx, chunk in enumerate(chunks, start=1):
            _logger.info("🔍 [EXTRATOR] Chunk %d/%d...", idx, total)
            try:
                batch = self._extract_chunk(chunk)
                all_transactions.extend(batch)
                _logger.info("✅ [EXTRATOR] Chunk %d: %d transação(ões) extraída(s).", idx, len(batch))
            except Exception as exc:
                _logger.error("❌ [EXTRATOR] Falha no chunk %d: %s", idx, exc)

        _logger.info("🏁 [EXTRATOR] Total extraído: %d transações.", len(all_transactions))
        return all_transactions

    # ── Internos ───────────────────────────────────────────────────────────────

    def _chunk_text(self, text: str) -> List[str]:
        """Filtra linhas em branco e agrupa em blocos de `lines_per_chunk` linhas."""
        lines = [line for line in text.splitlines() if line.strip()]
        return [
            "\n".join(lines[i : i + self.lines_per_chunk])
            for i in range(0, len(lines), self.lines_per_chunk)
        ]

    def _extract_chunk(self, chunk: str) -> List[ExtractedTransaction]:
        messages = [
            SystemMessage(content=_SYSTEM_PROMPT),
            HumanMessage(
                content=f"Extraia as transações financeiras do seguinte trecho:\n\n{chunk}"
            ),
        ]

        # Caminho 1: structured_output (LangChain gerencia o schema automaticamente)
        if self._structured_llm is not None:
            try:
                result: TransactionList = self._structured_llm.invoke(messages)
                return result.transacoes
            except Exception as exc:
                _logger.warning(
                    "⚠️  [EXTRATOR] structured_output falhou neste chunk (%s). "
                    "Tentando JsonOutputParser.",
                    exc,
                )

        # Caminho 2: fallback — parse manual com validação Pydantic
        response = self._llm.invoke(messages)
        return self._parse_fallback(response.content)

    def _parse_fallback(self, content: str) -> List[ExtractedTransaction]:
        """Tenta extrair JSON da resposta mesmo que o modelo devolva texto extra."""
        # Tenta extrair bloco JSON caso o modelo envolva em ```json ... ```
        match = re.search(r"\{.*\}", content, re.DOTALL)
        raw = match.group(0) if match else content

        data = json.loads(raw)
        transaction_list = TransactionList(**data)
        return transaction_list.transacoes
