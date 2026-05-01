import json
import os
import re

class KnowledgeBase:
    def __init__(self, file_path="knowledge_base.json"):
        self.file_path = file_path
        self.memory = self._load_memory()

    def _load_memory(self) -> dict:
        if os.path.exists(self.file_path):
            try:
                with open(self.file_path, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except Exception as e:
                print(f"Aviso: Não foi possível carregar a base. Erro: {e}")
                return {}
        return {}

    def _save_memory(self):
        with open(self.file_path, 'w', encoding='utf-8') as f:
            json.dump(self.memory, f, ensure_ascii=False, indent=4)

    def _normalize(self, text: str) -> str:
        text = str(text).lower().strip()
        return re.sub(r'\s+', ' ', text)

    def get_category(self, account_id: str, description: str):
        """Busca a regra específica daquele usuário/conta"""
        key = self._normalize(description)
        
        # Verifica se a conta existe na memória e se a regra existe para ela
        if account_id in self.memory:
            return self.memory[account_id].get(key)
        return None

    def add_rule(self, account_id: str, description: str, category_name: str):
        """Salva a regra dentro do 'cercadinho' daquele usuário/conta"""
        key = self._normalize(description)
        
        # Se a conta nunca foi vista, cria um espaço vazio para ela
        if account_id not in self.memory:
            self.memory[account_id] = {}

        # Salva a regra apenas na área desta conta
        if key not in self.memory[account_id] or self.memory[account_id][key] != category_name:
            self.memory[account_id][key] = category_name
            self._save_memory()