import json
import re
from langchain_community.llms import Ollama
from src.Domain.interfaces import ILlmClassifier

class OllamaClassifier(ILlmClassifier):
    def __init__(self, model_name="llama3.2:3b"):
        self.llm = Ollama(model=model_name, temperature=0.3, format="json")

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
        except:
            return {"categoryName": "Revisão Necessária", "isNew": True}


    def classify_batch(self, descriptions: list, existing_categories: list) -> list:
        if not descriptions:
            return []

        categories_str = ", ".join(existing_categories)
        lista_transacoes = "\n".join([f"{i+1}. '{desc}'" for i, desc in enumerate(descriptions)])
        
        prompt = f"""
        Você é um classificador financeiro inteligente. 
        Seu objetivo é retornar EXATAMENTE um ARRAY JSON puro.

        Categorias existentes:
        [{categories_str}]

        Transações para classificar:
        {lista_transacoes}

        REGRAS:
        1. Tente encaixar a transação nas categorias existentes.
        2. PROIBIDO TER PREGUIÇA: Evite ao máximo classificar como "Outros", "Diversos" ou "Geral".
        3. Se não houver uma categoria perfeita, CRIE uma nova (máximo 2 palavras) que vá direto ao ponto.
           Exemplos de raciocínio para NOVAS categorias:
           - "Imobiliaria Vitrini" ou "ALUGUEL VITRINI" -> "Aluguel"
           - "Cemig Distribuicao Sa" -> "Energia"
           - "Tim S A" ou "Claro" -> "Telefonia"
           - "Estado De Minas Gerais" -> "Impostos"
           - "Balada Bilheteria" -> "Entretenimento"
        4. O retorno DEVE SER APENAS O ARRAY JSON, sem formatação markdown, sem ```json e sem conversinha.

        Exemplo OBRIGATÓRIO de saída:
        [
            {{"description": "nome exato", "categoryName": "Categoria", "isNew": true}}
        ]
        """

        try:
            response = self.llm.invoke(prompt).strip()
            
            # Ajuste no Regex para lidar melhor com quebras de linha caso o Gemma se perca
            json_match = re.search(r'\[\s*\{.*?\}\s*\]', response, re.DOTALL)
            
            if json_match:
                return json.loads(json_match.group())
            
            raise ValueError("Array JSON não encontrado na resposta.")
            
        except Exception as e:
            print(f"Erro no classify_batch: {e}")
            return [{"description": desc, "categoryName": "Revisão Necessária", "isNew": True} for desc in descriptions]