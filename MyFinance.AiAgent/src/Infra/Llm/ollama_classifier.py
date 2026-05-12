import json
import re
from langchain_community.llms import Ollama
from src.Domain.interfaces import ILlmClassifier

class OllamaClassifier(ILlmClassifier):
    def __init__(self, model_name="llama3.2:3b"):
        self.llm = Ollama(model=model_name, temperature=0.1, format="json")

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
        
        # Mudança 1: Pedir um Objeto JSON com a chave "resultados"
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
            
            # Mudança 2: Limpeza robusta de Markdown caso o modelo retorne ```json ... ```
            if response.startswith("```json"):
                response = response[7:]
            elif response.startswith("```"):
                response = response[3:]
                
            if response.endswith("```"):
                response = response[:-3]
                
            response = response.strip()

            # Mudança 3: Tenta converter direto primeiro, sem regex, pois já limpamos as bordas
            try:
                data = json.loads(response)
                # Retorna o array que está dentro da chave "resultados"
                return data.get("resultados", [])
            except json.JSONDecodeError:
                # Mudança 4: Fallback com Regex buscando o objeto raiz {...} em vez de [...]
                json_match = re.search(r'\{.*\}', response, re.DOTALL)
                if json_match:
                    data = json.loads(json_match.group())
                    return data.get("resultados", [])
                raise ValueError("Objeto JSON não encontrado na resposta.")
            
        except Exception as e:
            print(f"Erro no classify_batch: {e}\nResposta original do LLM:\n{response}")
            return [{"description": desc, "categoryName": "Revisão Necessária", "isNew": True} for desc in descriptions]