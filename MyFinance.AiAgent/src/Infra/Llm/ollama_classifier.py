import json
import re
from langchain_ollama import OllamaLLM
from src.Domain.interfaces import ILlmClassifier
from src.Infra.Llm.ollama_provider import get_ollama_config, get_model

_BATCH_SIZE = 15

# Vocabulário base de categorias financeiras.
# Amplia as opções do modelo para reduzir alucinações sem impedir criação de novas.
# O sistema ainda trata como "sugestão" qualquer categoria não criada pelo usuário.
_SEED_CATEGORIES = [
    "Alimentação", "Transporte", "Saúde", "Educação", "Lazer",
    "Moradia", "Vestuário", "Salário", "Investimentos", "Fatura Cartão",
    "Transferência", "Serviços", "Assinaturas", "Pet", "Beleza",
    "Seguro", "Farmácia", "Combustível", "Streaming", "Família",
]


class OllamaClassifier(ILlmClassifier):
    def __init__(self):
        self.llm = OllamaLLM(
            model=get_model("classifier"),
            temperature=0.2,
            format="json",
            **get_ollama_config(),
        )

    def _strip_markdown(self, text: str) -> str:
        if text.startswith("```json"):
            text = text[7:]
        elif text.startswith("```"):
            text = text[3:]
        if text.endswith("```"):
            text = text[:-3]
        return text.strip()

    def _fix_is_new(self, result: dict, existing_categories_lower: set) -> dict:
        """Garante que isNew reflete a realidade: categoria não existe na lista = isNew True."""
        name = result.get("categoryName", "")
        result["isNew"] = name.strip().lower() not in existing_categories_lower
        return result

    def _expand_categories(self, existing_categories: list) -> str:
        """Combina categorias do usuário com o vocabulário-base, sem duplicatas."""
        existing_lower = {c.lower() for c in existing_categories}
        seeds = [s for s in _SEED_CATEGORIES if s.lower() not in existing_lower]
        combined = existing_categories + seeds
        return ", ".join(combined)

    def classify_category(self, description: str, existing_categories: list) -> dict:
        categories_str = self._expand_categories(existing_categories)
        existing_lower = {c.lower() for c in existing_categories}

        prompt = f"""Você é um classificador de transações financeiras. Responda APENAS com JSON puro.

Transação: "{description}"

Categorias disponíveis: [{categories_str}]

Decisão:
- Se a transação se encaixa CLARAMENTE em uma categoria disponível: use-a com isNew=false.
- Se nenhuma categoria disponível descreve bem a transação: INVENTE uma nova (máximo 2 palavras, substantivo direto) com isNew=true.

Formato de saída obrigatório:
{{"categoryName": "Nome", "isNew": true}}"""

        try:
            response = self._strip_markdown(self.llm.invoke(prompt))
            json_match = re.search(r'\{.*\}', response, re.DOTALL)
            if json_match:
                result = json.loads(json_match.group())
                return self._fix_is_new(result, existing_lower)
            return {"categoryName": "Outros", "isNew": False}
        except Exception:
            return {"categoryName": "Revisão Necessária", "isNew": True}

    def _classify_chunk(self, descriptions: list, categories_str: str, existing_lower: set) -> list:
        lista_transacoes = "\n".join([f"{i+1}. \"{desc}\"" for i, desc in enumerate(descriptions)])

        prompt = f"""Você é um classificador de transações financeiras. Responda APENAS com JSON puro.

Categorias disponíveis: [{categories_str}]

Transações:
{lista_transacoes}

Para cada transação escolha a categoria mais específica e adequada da lista.
Se nenhuma categoria descreve bem a transação, crie um nome novo (máximo 2 palavras).
NUNCA use "Outros", "Diversos" ou "Geral".

Retorne o JSON com exatamente {len(descriptions)} resultados:
{{"resultados": [{{"description": "texto original", "categoryName": "Categoria", "isNew": false}}]}}"""

        response = self._strip_markdown(self.llm.invoke(prompt))

        try:
            data = json.loads(response)
        except json.JSONDecodeError:
            json_match = re.search(r'\{.*\}', response, re.DOTALL)
            if not json_match:
                raise ValueError("JSON não encontrado na resposta.")
            data = json.loads(json_match.group())

        results = data.get("resultados", [])
        return [self._fix_is_new(r, existing_lower) for r in results]

    def classify_batch(self, descriptions: list, existing_categories: list) -> list:
        if not descriptions:
            return []

        categories_str = ", ".join(existing_categories)
        existing_lower = {c.lower() for c in existing_categories}
        all_results = []

        chunks = [descriptions[i:i + _BATCH_SIZE] for i in range(0, len(descriptions), _BATCH_SIZE)]

        for chunk in chunks:
            try:
                results = self._classify_chunk(chunk, categories_str, existing_lower)
                all_results.extend(results)
            except Exception as e:
                print(f"Erro no classify_batch (chunk): {e}")
                all_results.extend([
                    {"description": desc, "categoryName": "Revisão Necessária", "isNew": True}
                    for desc in chunk
                ])

        return all_results
