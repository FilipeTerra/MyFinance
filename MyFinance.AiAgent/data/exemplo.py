"""
exemplo.py — Cliente Python para o servidor Ollama da disciplina
Modelos disponíveis: deepseek-r1:8b, mxbai-embed-large, llama3.2:3b,
                     llama3.1:8b, nomic-embed-text, qwen2.5:7b,
                     mixtral:8x7b, StarCoder, mistral:7b-instruct,
                     llama3, deepseek-coder

Configuração necessária: preencha BASE_URL e API_KEY abaixo.
"""

import json
import requests

# ─── Configuração ─────────────────────────────────────────────────────────────

BASE_URL = "http://ollama.futurelab.dcc.ufmg.br"  # Endereço do proxy da disciplina
API_KEY  = "pedrobarrosnvk58n6f"             # Chave individual fornecida pelo monitor

HEADERS = {
    "Content-Type": "application/json",
    "X-API-Key": API_KEY,                # Obrigatório — todas as requisições ao proxy
}

MODELO_TEXTO     = "llama3.2:3b"         # Rápido, bom para testes
MODELO_RACIOCINIO = "deepseek-r1:8b"     # Melhor para problemas complexos
MODELO_CODIGO    = "deepseek-coder:latest"
MODELO_EMBED     = "nomic-embed-text:latest"


# ─── Funções utilitárias ──────────────────────────────────────────────────────

def check_api() -> None:
    """
    Verifica se a API key e o servidor estão configurados corretamente.
    Deve ser chamada antes de qualquer outra função.
    Interrompe a execução com uma mensagem clara se algo estiver errado.
    """
    print("Verificando conexão com o servidor...", end=" ", flush=True)

    # 1. Detecta placeholders não substituídos
    if "SEU_IP" in BASE_URL:
        print("✗")
        raise SystemExit(
            "\n[ERRO] BASE_URL não configurada.\n"
            "       Substitua 'SEU_IP_PUBLICO' pelo endereço fornecido pelo monitor.\n"
            f"       Atual: {BASE_URL}"
        )
    if API_KEY == "SUA_CHAVE_AQUI":
        print("✗")
        raise SystemExit(
            "\n[ERRO] API_KEY não configurada.\n"
            "       Substitua 'SUA_CHAVE_AQUI' pela chave individual fornecida pelo monitor."
        )

    # 2. Tenta conectar ao servidor
    try:
        requests.get(f"{BASE_URL}/health", timeout=5)
    except requests.exceptions.ConnectionError:
        print("✗")
        raise SystemExit(
            f"\n[ERRO] Não foi possível conectar ao servidor.\n"
            f"       Verifique se o endereço está correto: {BASE_URL}\n"
            f"       O servidor pode estar fora do ar — contate o monitor."
        )
    except requests.exceptions.Timeout:
        print("✗")
        raise SystemExit(
            f"\n[ERRO] O servidor demorou demais para responder (timeout).\n"
            f"       Verifique sua conexão com a internet e tente novamente."
        )

    # 3. Verifica a API key com uma rota autenticada
    r_auth = requests.get(f"{BASE_URL}/api/tags", headers=HEADERS)

    if r_auth.status_code == 401:
        print("✗")
        detail = r_auth.json().get("detail", {})
        raise SystemExit(
            f"\n[ERRO 401] {detail.get('error', 'API key ausente')}\n"
            f"           {detail.get('message', '')}\n"
            f"           Exemplo: {detail.get('exemplo', 'X-API-Key: sua-chave')}"
        )

    if r_auth.status_code == 403:
        print("✗")
        detail = r_auth.json().get("detail", {})
        raise SystemExit(
            f"\n[ERRO 403] {detail.get('error', 'API key inválida')}\n"
            f"           {detail.get('message', '')}\n"
            f"           Chave usada: {detail.get('recebido', API_KEY)}\n"
            f"           {detail.get('dica', '')}"
        )

    if r_auth.status_code != 200:
        print("✗")
        raise SystemExit(
            f"\n[ERRO {r_auth.status_code}] Resposta inesperada do servidor.\n"
            f"           Conteúdo: {r_auth.text[:200]}"
        )

    # 4. Tudo certo
    modelos = [m["name"] for m in r_auth.json().get("models", [])]
    print("✓")
    print(f"  Servidor  : {BASE_URL}")
    print(f"  Modelos   : {', '.join(modelos)}")
    print()


def listar_modelos() -> list[str]:
    """Retorna os nomes de todos os modelos disponíveis."""
    r = requests.get(f"{BASE_URL}/api/tags", headers=HEADERS)
    r.raise_for_status()
    return [m["name"] for m in r.json().get("models", [])]


def gerar_texto(prompt: str, modelo: str = MODELO_TEXTO) -> str:
    """Gera texto a partir de um prompt simples (sem histórico)."""
    payload = {"model": modelo, "prompt": prompt, "stream": False}
    r = requests.post(f"{BASE_URL}/api/generate", headers=HEADERS, json=payload)
    r.raise_for_status()
    data = r.json()
    if "error" in data:
        raise RuntimeError(f"Ollama erro: {data['error']}")
    return data["response"]


def gerar_texto_stream(prompt: str, modelo: str = MODELO_TEXTO) -> None:
    """Gera texto com streaming — imprime token a token na tela."""
    payload = {"model": modelo, "prompt": prompt, "stream": True}
    with requests.post(f"{BASE_URL}/api/generate", headers=HEADERS,
                       json=payload, stream=True) as r:
        r.raise_for_status()
        for line in r.iter_lines():
            if line:
                data = json.loads(line)
                print(data.get("response", ""), end="", flush=True)
                if data.get("done"):
                    print()
                    break


def chat(mensagens: list[dict], modelo: str = MODELO_TEXTO) -> str:
    """
    Chat com histórico de mensagens.

    Formato de mensagens:
        [
            {"role": "system", "content": "Você é um assistente."},
            {"role": "user",   "content": "Pergunta aqui"},
            {"role": "assistant", "content": "Resposta anterior"},  # opcional
            {"role": "user",   "content": "Próxima pergunta"},
        ]
    """
    payload = {"model": modelo, "messages": mensagens, "stream": False}
    r = requests.post(f"{BASE_URL}/api/chat", headers=HEADERS, json=payload)
    r.raise_for_status()
    data = r.json()
    if "error" in data:
        raise RuntimeError(f"Ollama erro: {data['error']}")
    return data["message"]["content"]


def chat_stream(mensagens: list[dict], modelo: str = MODELO_TEXTO) -> None:
    """Chat com streaming."""
    payload = {"model": modelo, "messages": mensagens, "stream": True}
    with requests.post(f"{BASE_URL}/api/chat", headers=HEADERS,
                       json=payload, stream=True) as r:
        r.raise_for_status()
        for line in r.iter_lines():
            if line:
                data = json.loads(line)
                if "message" in data:
                    print(data["message"].get("content", ""), end="", flush=True)
                if data.get("done"):
                    print()
                    break


def gerar_embedding(texto: str, modelo: str = MODELO_EMBED) -> list[float]:
    """Gera um vetor de embedding para o texto fornecido."""
    payload = {"model": modelo, "prompt": texto}
    r = requests.post(f"{BASE_URL}/api/embeddings", headers=HEADERS, json=payload)
    r.raise_for_status()
    data = r.json()
    if "error" in data:
        raise RuntimeError(f"Ollama erro: {data['error']}")
    return data["embedding"]


def similaridade_cosseno(v1: list[float], v2: list[float]) -> float:
    """Calcula similaridade de cosseno entre dois vetores (sem numpy)."""
    dot   = sum(a * b for a, b in zip(v1, v2))
    norm1 = sum(a ** 2 for a in v1) ** 0.5
    norm2 = sum(b ** 2 for b in v2) ** 0.5
    return dot / (norm1 * norm2) if norm1 and norm2 else 0.0


def divisor(titulo: str) -> None:
    print(f"\n{'─' * 60}")
    print(f"  {titulo}")
    print(f"{'─' * 60}")


# ─── Exemplos ─────────────────────────────────────────────────────────────────

if __name__ == "__main__":

    # 0. Verifica conexão e API key antes de qualquer coisa
    check_api()

    # 1. Modelos disponíveis
    divisor("1. Modelos disponíveis")
    for m in listar_modelos():
        print(f"  • {m}")

    # 2. Geração simples
    divisor("2. Geração simples")
    resposta = gerar_texto(
        "O que é um agente de IA? Responda em 2 frases.",
        modelo=MODELO_TEXTO,
    )
    print(resposta)

    # 3. Streaming de texto
    divisor("3. Streaming de texto (token a token)")
    gerar_texto_stream(
        "Explique o conceito de memória em agentes de IA de forma didática.",
        modelo=MODELO_TEXTO,
    )

    # 4. Chat simples
    divisor("4. Chat com system prompt")
    resposta = chat([
        {"role": "system", "content": "Você é um tutor de IA direto e didático. "
                                       "Responda sempre em no máximo 3 linhas."},
        {"role": "user",   "content": "Qual a diferença entre RAG e fine-tuning?"},
    ])
    print(resposta)

    # 5. Chat com múltiplas turnos (histórico manual)
    divisor("5. Chat com histórico de múltiplos turnos")
    historico = [
        {"role": "system",    "content": "Você é um assistente de programação Python."},
        {"role": "user",      "content": "O que é uma list comprehension?"},
    ]
    r1 = chat(historico)
    print(f"[Turno 1]\n{r1}")

    historico += [
        {"role": "assistant", "content": r1},
        {"role": "user",      "content": "Me dê um exemplo com filtro de números pares."},
    ]
    r2 = chat(historico)
    print(f"\n[Turno 2]\n{r2}")

    # 6. Chat com streaming
    divisor("6. Chat com streaming")
    chat_stream([
        {"role": "system", "content": "Você é um assistente conciso."},
        {"role": "user",   "content": "Quais são os 3 principais desafios de agentes LLM?"},
    ])

    # 7. Raciocínio com deepseek-r1
    divisor("7. Raciocínio passo a passo (deepseek-r1:8b)")
    resposta = gerar_texto(
        "Um agente tem 3 ferramentas: busca web, calculadora e banco de dados. "
        "O usuário pergunta: 'Qual o PIB do Brasil em 2023 dividido por 2?' "
        "Descreva a sequência de ações que o agente deve tomar.",
        modelo=MODELO_RACIOCINIO,
    )
    print(resposta)

    # 8. Geração de código
    divisor("8. Geração de código (deepseek-coder)")
    codigo = gerar_texto(
        "Escreva uma função Python que recebe uma lista de strings e retorna "
        "apenas as que têm mais de 5 palavras. Inclua docstring e exemplo de uso.",
        modelo=MODELO_CODIGO,
    )
    print(codigo)

    # 9. Embeddings e similaridade semântica
    divisor("9. Embeddings e similaridade semântica")
    frases = [
        "Redes neurais aprendem padrões a partir de dados.",
        "Deep learning é baseado em camadas de neurônios artificiais.",
        "O Brasil é o maior país da América do Sul.",
    ]
    embeddings = [gerar_embedding(f) for f in frases]
    dim = len(embeddings[0])
    print(f"Dimensão dos vetores: {dim}")

    ref = 0
    print(f"\nSimilaridade com: \"{frases[ref]}\"")
    for i, frase in enumerate(frases):
        if i == ref:
            continue
        sim = similaridade_cosseno(embeddings[ref], embeddings[i])
        print(f"  [{sim:.4f}] {frase}")

    # 10. Exemplo de pipeline RAG mínimo
    divisor("10. Pipeline RAG mínimo (busca semântica + geração)")
    base_conhecimento = [
        "LangChain é um framework para construir aplicações com LLMs.",
        "LlamaIndex especializa-se em indexação e busca sobre documentos.",
        "CrewAI permite criar times de agentes com papéis definidos.",
        "AutoGen é um framework da Microsoft para agentes conversacionais.",
        "O modelo GPT-4 foi lançado pela OpenAI em março de 2023.",
    ]

    pergunta = "Qual framework posso usar para criar agentes com papéis?"

    # Gera embeddings para a base e para a pergunta
    emb_base     = [gerar_embedding(doc) for doc in base_conhecimento]
    emb_pergunta = gerar_embedding(pergunta)

    # Encontra o documento mais similar
    scores = [(similaridade_cosseno(emb_pergunta, emb), doc)
              for emb, doc in zip(emb_base, base_conhecimento)]
    scores.sort(reverse=True)
    doc_relevante = scores[0][1]

    print(f"Pergunta:         {pergunta}")
    print(f"Doc recuperado:   {doc_relevante} (score={scores[0][0]:.4f})")

    resposta_rag = chat([
        {"role": "system", "content": "Responda apenas com base no contexto fornecido."},
        {"role": "user",   "content": f"Contexto: {doc_relevante}\n\nPergunta: {pergunta}"},
    ])
    print(f"Resposta gerada:  {resposta_rag}")
