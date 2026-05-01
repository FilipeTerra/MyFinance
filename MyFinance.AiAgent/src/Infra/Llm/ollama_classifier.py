import json
import re
from langchain_community.llms import Ollama
from src.Domain.interfaces import ILlmClassifier

class OllamaClassifier(ILlmClassifier):
    def __init__(self, model_name="gemma4"):
        self.llm = Ollama(model=model_name, temperature=0)

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
        
        # Numera e formata as transações para a IA não se perder
        lista_transacoes = "\n".join([f"{i+1}. '{desc}'" for i, desc in enumerate(descriptions)])
        
        # A Engenharia de Prompt aqui é super rígida para forçar a IA a devolver um Array
        prompt = f"""
        Você é um sistema de classificação financeira avançado. 
        Categorias existentes: [{categories_str}]
        
        Sua tarefa é analisar uma lista de transações e retornar EXATAMENTE um ARRAY JSON puro.
        
        Regras:
        1. Para cada transação, escolha uma categoria existente ou sugira uma NOVA (máx 2 palavras).
        2. A sua resposta DEVE SER APENAS O ARRAY JSON. Não escreva 'Aqui está', não use blocos de código (```json).
        
        Formato OBRIGATÓRIO de saída:
        [
          {{"description": "nome exato da transacao", "categoryName": "Nome da Categoria", "isNew": true/false}}
        ]
        
        Transações para analisar:
        {lista_transacoes}
        """

        try:
            # Envia a lista gigante de uma vez só
            response = self.llm.invoke(prompt).strip()
            
            # Usamos Regex para capturar tudo que estiver entre colchetes [ ] (o nosso Array)
            json_match = re.search(r'\[.*\]', response, re.DOTALL)
            
            if json_match:
                resultados = json.loads(json_match.group())
                return resultados
            
            raise ValueError("Array JSON não encontrado na resposta.")
            
        except Exception as e:
            print(f"Erro no classify_batch: {e}")
            # Se a IA falhar em entender ou gerar um JSON inválido, não quebramos a API
            return [{"description": desc, "categoryName": "Revisão Necessária", "isNew": True} for desc in descriptions]