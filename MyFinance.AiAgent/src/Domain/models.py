from __future__ import annotations
from typing import List, Literal, Optional
from pydantic import BaseModel, Field


class ExtractedTransaction(BaseModel):
    data: str = Field(description="Data da transação no formato YYYY-MM-DD")
    descricao: str = Field(description="Descrição da transação")
    valor: float = Field(description="Valor absoluto da transação (sempre positivo)")
    tipo: Literal["receita", "despesa"] = Field(
        description="Tipo: 'receita' para entradas/créditos, 'despesa' para saídas/débitos"
    )
    categoria: Optional[str] = Field(
        default=None,
        description="Categoria explícita no extrato (ex: 'Alimentação', 'Transporte'). null se não informada.",
    )


class TransactionList(BaseModel):
    transacoes: List[ExtractedTransaction] = Field(
        default_factory=list,
        description="Lista de transações financeiras extraídas do documento",
    )
