# Imagem de execução do AiAgent (FastAPI) para deploy no Render.
FROM python:3.11-slim

WORKDIR /app

# Dependências nativas usadas por faiss-cpu/pdfplumber quando não há wheel
# pronto para a plataforma do build.
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Render injeta a porta real via $PORT; 8181 é o fallback para rodar local.
ENV PORT=8181
EXPOSE 8181

CMD ["sh", "-c", "uvicorn src.Api.main:app --host 0.0.0.0 --port ${PORT}"]
