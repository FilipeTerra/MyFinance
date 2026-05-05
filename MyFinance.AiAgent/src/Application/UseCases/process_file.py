import pandas as pd
from datetime import datetime
from src.Domain.interfaces import IFileExtractor, ILlmClassifier
from src.Infra.Cache.knowledge_base import KnowledgeBase

class ProcessFileUseCase:
    def __init__(self, extractor: IFileExtractor, classifier: ILlmClassifier):
        self.extractor = extractor
        self.classifier = classifier
        self.kb = KnowledgeBase()

    def execute(self, file_path: str, account_id: str, existing_categories: dict) -> list:
        df = self.extractor.extract(file_path)
        category_names = list(existing_categories.keys())
        
        resultados_finais = []
        transacoes_pendentes = []

        print(f"\n--- Iniciando triagem de {len(df)} transações ---")

        for index, row in df.iterrows():
            try:
                data_str = str(row['data']).strip()
                try:
                    data_obj = datetime.strptime(data_str, "%d/%m/%Y")
                    data_iso = data_obj.strftime("%Y-%m-%d")
                except ValueError:
                    data_iso = data_str

                valor_str = str(row['valor']).strip()

                # Tira o ponto de milhar (se houver) e troca a vírgula decimal por ponto
                valor_str = valor_str.replace('.', '').replace(',', '.')

                valor_float = float(valor_str)
                descricao = str(row['descricao']).strip()

                # Consulta a Memória Rápida
                categoria_cache = self.kb.get_category(account_id, descricao)

                if categoria_cache:
                    print(f"[RÁPIDO] '{descricao}' -> {categoria_cache}")
                    category_id = existing_categories.get(categoria_cache)
                    resultados_finais.append({
                        "date": data_iso,
                        "description": descricao,
                        "amount": valor_float,
                        "accountId": account_id,
                        "categoryId": category_id,
                        "suggestedCategoryName": None,
                        "isSuggestion": False
                    })
                else:
                    print(f"[NOVA] '{descricao}' separada para análise da IA.")
                    transacoes_pendentes.append({
                        "date": data_iso,
                        "description": descricao,
                        "amount": valor_float,
                        "accountId": account_id
                    })

            except Exception as e:
                print(f"Erro ao processar a linha {index}: {e}")
                continue
        
        if transacoes_pendentes:
            print(f"\n--- Enviando lote de {len(transacoes_pendentes)} transações para a IA ---")
            
            # Extraímos apenas as descrições para mandar para o Gemma
            descricoes = [t['description'] for t in transacoes_pendentes]
            
            # Uma única chamada à IA!
            respostas_ia = self.classifier.classify_batch(descricoes, category_names)
            
            # Criamos um "dicionário de busca rápida" com a resposta da IA. 
            # Fazemos isso porque a IA pode responder fora de ordem!
            mapa_respostas = {item['description']: item for item in respostas_ia if 'description' in item}

            # Agora varremos as pendentes e casamos com a resposta da IA
            for t in transacoes_pendentes:
                desc = t['description']
                
                # Pega a decisão da IA (ou usa um fallback caso a IA tenha esquecido esse item)
                decisao = mapa_respostas.get(desc, {"categoryName": "Revisão Necessária", "isNew": True})
                cat_name = decisao.get('categoryName', 'Revisão Necessária')
                is_new = decisao.get('isNew', True)

                # Ensinamos a base de conhecimento!
                self.kb.add_rule(account_id, desc, cat_name)
                print(f"Aprendi: '{desc}' agora é '{cat_name}'")

                category_id = existing_categories.get(cat_name)
                
                # Validação: se a categoria não existe mas IA disse que não é nova, corrigir
                if category_id is None and not is_new:
                    is_new = True
                    print(f"Ajuste: '{cat_name}' não encontrada em categorias existentes, marcando como nova sugestão.")
                
                resultados_finais.append({
                    "date": t['date'],
                    "description": desc,
                    "amount": t['amount'],
                    "accountId": account_id,
                    "categoryId": category_id,
                    "suggestedCategoryName": cat_name if is_new else None,
                    "isSuggestion": is_new
                })

        print("\n✅ Processamento concluído com sucesso!")
        return resultados_finais