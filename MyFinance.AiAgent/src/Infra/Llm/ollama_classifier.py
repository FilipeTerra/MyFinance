import json
import re
from langchain_ollama import OllamaLLM
from src.Domain.interfaces import ILlmClassifier

# SLM rápido e determinístico para tarefas de classificação em background
_MODEL_NAME = "llama3.2:3b"


class OllamaClassifier(ILlmClassifier):
    def __init__(self):
        self.llm = OllamaLLM(model=_MODEL_NAME, temperature=0.1, format="json")

    def classify_category(self, description: str, existing_categories: list) -> dict:
        categories_str = ", ".join(existing_categories)
        prompt = f"""
        Você é um sistema de classificação financeira.
        Analise a transação: '{description}'

        Categorias existentes: [{categories_str}]

        Regras:
        1. Se a transação pertencer a uma categoria existente, escolha-a.
        2. Se for ambíguo ou novo, sugira uma NOVA categoria (máximo 2 palavras).
        3. Responda APENAS com um JSON puro no formato:
        {{"categoryName": "Nome", "isNew": true/false}}
        """
        try:
            response = self.llm.invoke(prompt).strip()
            json_match = re.search(r'\{.*\}', response, re.DOTALL)
            if json_match:
                return json.loads(json_match.group())
            return {"categoryName": "Outros", "isNew": False}
        except Exception:
            return {"categoryName": "Revisão Necessária", "isNew": True}

    def classify_batch(self, descriptions: list, existing_categories: list) -> list:
        if not descriptions:
            return []

        categories_str = ", ".join(existing_categories)
        lista_transacoes = "\n".join([f"{i+1}. '{desc}'" for i, desc in enumerate(descriptions)])

        prompt = f"""
        Você é um classificador financeiro inteligente.
        Seu objetivo é retornar EXATAMENTE um Objeto JSON.

        Categorias existentes:
        [{categories_str}]

        Transações para classificar:
        {lista_transacoes}

        REGRAS:
        1. Tente encaixar a transação nas categorias existentes.
        2. PROIBIDO TER PREGUIÇA: Evite ao máximo classificar como "Outros", "Diversos" ou "Geral".
        3. Se não houver uma categoria perfeita, CRIE uma nova (máximo 2 palavras) que vá direto ao ponto.
        4. O retorno DEVE SER APENAS O OBJETO JSON, sem formatação markdown e sem conversinha.

        Exemplo OBRIGATÓRIO de saída:
        {{
            "resultados": [
                {{"description": "nome exato", "categoryName": "Categoria", "isNew": true}}
            ]
        }}
        """

        try:
            response = self.llm.invoke(prompt).strip()

            if response.startswith("```json"):
                response = response[7:]
            elif response.startswith("```"):
                response = response[3:]
            if response.endswith("```"):
                response = response[:-3]
            response = response.strip()

            try:
                data = json.loads(response)
                return data.get("resultados", [])
            except json.JSONDecodeError:
                json_match = re.search(r'\{.*\}', response, re.DOTALL)
                if json_match:
                    data = json.loads(json_match.group())
                    return data.get("resultados", [])
                raise ValueError("Objeto JSON não encontrado na resposta.")

        except Exception as e:
            print(f"Erro no classify_batch: {e}\nResposta original do LLM:\n{response}")
            return [{"description": desc, "categoryName": "Revisão Necessária", "isNew": True} for desc in descriptions]
