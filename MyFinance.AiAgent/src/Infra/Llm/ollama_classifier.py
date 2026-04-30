# MyFinance.AiAgent/src/Infra/Llm/ollama_classifier.py
import json
import re
from langchain_community.llms import Ollama
from src.Domain.interfaces import ILlmClassifier

class OllamaClassifier(ILlmClassifier):
    def __init__(self, model_name="gemma4"):
        self.llm = Ollama(model=model_name, temperature=0) # Temp 0 para consistência

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
            # Limpeza de possíveis comentários da IA
            json_match = re.search(r'\{.*\}', response, re.DOTALL)
            if json_match:
                return json.loads(json_match.group())
            return {"categoryName": "Outros", "isNew": False}
        except:
            return {"categoryName": "Revisão Necessária", "isNew": True}