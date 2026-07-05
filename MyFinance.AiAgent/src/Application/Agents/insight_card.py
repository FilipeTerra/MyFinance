"""
insight_card.py — Value object compartilhado pelos agentes proativos de insight
(reserva de emergência, lifestyle monitor).

Os dois agentes montam cards de dashboard no mesmo formato pedagógico: 3 blocos
curtos (curiosidade / informação / sugestão), números sempre calculados em
Python, nunca pelo LLM. Antes cada agente remontava esse dict e o payload de
erro {"success": False, "erro": ...} de forma independente; agora ambos vêm
de um único lugar.
"""
from dataclasses import dataclass


@dataclass(frozen=True)
class InsightCard:
    curiosidade: str
    informacao: str
    sugestao: str

    def to_dict(self) -> dict:
        return {
            "curiosidade": self.curiosidade,
            "informacao": self.informacao,
            "sugestao": self.sugestao,
        }


def erro_result(mensagem: str) -> dict:
    """Payload padrão de falha dos agentes proativos."""
    return {"success": False, "erro": mensagem}
