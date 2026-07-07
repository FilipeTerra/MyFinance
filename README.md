<p align="center">
  <img src="MyFinance.Frontend/public/FinAI%20logo.png" alt="FinAI logo" width="520">
</p>

# FinAI

> **Sistema Multiagentes de IA em Plataforma de Gestão Financeira Pessoal**

[![CI](https://github.com/FilipeTerra/MyFinance/actions/workflows/ci.yml/badge.svg)](https://github.com/FilipeTerra/MyFinance/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![Python](https://img.shields.io/badge/Python-3.11-3776AB?logo=python&logoColor=white)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)

---

## Sobre o Projeto

O **FinAI** nasceu da união de dois objetivos pessoais: a necessidade de manter as finanças organizadas e o desejo de evoluir tecnicamente.

Mais do que uma ferramenta de controle de gastos, é um laboratório de boas práticas de engenharia de software e arquitetura de agentes de IA. O projeto aplica **Clean Architecture**, design patterns, integração com **modelos de linguagem (LLMs)** e uma cultura de **testes automatizados + CI** para resolver um problema real do dia a dia com uma solução robusta e escalável.

O diferencial está na camada de **Agentes de IA**: em vez de apenas registrar transações, o sistema analisa o comportamento financeiro do usuário e gera insights educativos e oferece um consultor de IA embasado em literatura de finanças pessoais (via RAG).

---

## Arquitetura

O sistema é composto por **três serviços independentes** que se comunicam via HTTP:

```
┌─────────────────────┐        ┌───────────────────────────┐         ┌───────────────────────────┐
│  MyFinance.Frontend │        │      MyFinance.Api        │         │     MyFinance.AiAgent     │
│  React + Vite (SPA) │ ─────▶│   ASP.NET Core (REST)     │ ─────▶ │   FastAPI + LangGraph     │
│                     │  HTTP  │   Clean Architecture      │  HTTP   │   Agentes de IA / RAG     │
└─────────────────────┘        └───────────────────────────┘         └───────────────────────────┘
                                          │                                       │
                                          ▼                                       ▼
                                   ┌──────────────┐                       ┌──────────────┐
                                   │  PostgreSQL  │                       │    Ollama    │
                                   │  (EF Core)   │                       │ (LLM local/  │
                                   └──────────────┘                       │   remoto)    │
                                                                          └──────────────┘
```

### Backend .NET — Clean Architecture

A API segue a regra de dependência da Clean Architecture, com as camadas apontando sempre para o centro (Domain):

| Camada | Responsabilidade |
|---|---|
| **MyFinance.Domain** | Entidades e regras de negócio corporativas. Sem dependências externas. |
| **MyFinance.Application** | Casos de uso (Services), DTOs e interfaces (Repositories, Services). Depende apenas de Domain. |
| **MyFinance.Infrastructure** | Implementações concretas: EF Core, repositórios, hashing (BCrypt), geração de JWT, cliente HTTP do AiAgent. |
| **MyFinance.Api** | Composition root: Controllers, injeção de dependência, autenticação JWT, Swagger. |

Detalhes de tecnologia (BCrypt, JWT, HTTP) ficam isolados atrás de abstrações (`IPasswordHasher`, `ITokenService`, `IAiIntegrationService`), mantendo o núcleo de negócio livre de acoplamento a frameworks.

### AiAgent Python — Agentes e RAG

Serviço FastAPI que orquestra agentes de IA com **LangGraph**, também organizado em camadas (`Domain`, `Application`, `Infra`):

- **Consultor conversacional (padrão ReAct):** chat que raciocina em ciclos *Thought → Action → Observation*, decidindo quais tools chamar a cada pergunta e mantendo memória da conversa por usuário.
- **Agentes proativos** que analisam o histórico financeiro e decidem quando exibir insights (reserva de emergência, inflação do estilo de vida).
- **RAG** (Retrieval-Augmented Generation) com **FAISS** sobre uma base de livros de finanças pessoais, para embasar sugestões com fontes citáveis.
- **Providers flexíveis de LLM** via Ollama, com fallback automático de um proxy remoto para uma instância local.

**Tools do agente consultor:**

| Categoria | Exemplos |
|---|---|
| Dados da conta (API .NET) | saldos, transações recentes, gastos por categoria, resumo financeiro, metas, investimentos, perfil |
| Simulações | criar/aportar meta, simular meta ideal, simular impacto de nova despesa, simular investimento, juros de financiamento |
| Dados de mercado (tempo real) | taxa Selic, indicadores de ações na B3 |
| Conhecimento (RAG) | consulta à base de livros de finanças pessoais |

---

## Funcionalidades

* **Autenticação:** Registro e login com senha protegida por hash (BCrypt) e autenticação via JWT.
* **Contas:** CRUD de contas financeiras (corrente, poupança, carteira, cartão, investimento) com controle de saldo.
* **Transações:** Registro de receitas, despesas e aportes, com ajuste automático de saldo e importação em lote a partir de extratos.
* **Categorias:** Organização das transações por categoria personalizada.
* **Metas Financeiras:** Criação de metas com acompanhamento de progresso e aportes.
* **Investimentos:** Registro de ativos (Renda Fixa, Ações, FIIs, Cripto, ETFs) com acompanhamento de rentabilidade.
* **Consultor Financeiro (Chat):** agente conversacional que responde perguntas sobre a vida financeira do usuário em linguagem natural, com acesso a dados reais da conta e a mais de 15 tools (consultas, simulações, mercado e conhecimento).
* **Insights de IA (proativos):**
  * *Reserva de Emergência* — avalia se o valor guardado atinge o ideal recomendado.
  * *Inflação do Estilo de Vida* — detecta se gastos supérfluos crescem mais rápido que os investimentos.
* **Processamento de extratos por IA:** Extração automática de transações a partir de arquivos.

---

## Stack Tecnológica

### Backend — API (.NET 9)
* **C# / ASP.NET Core** — API RESTful
* **Entity Framework Core** + **PostgreSQL (Npgsql)** — persistência
* **JWT Bearer** — autenticação
* **BCrypt.Net** — hashing de senhas
* **Swagger / OpenAPI** — documentação da API

### AiAgent (Python 3.11)
* **FastAPI** — serviço HTTP
* **LangChain + LangGraph** — orquestração de agentes
* **FAISS** — vector store para RAG
* **Ollama** — execução de LLMs (local ou proxy remoto)
* **pandas / pdfplumber** — processamento de extratos

### Frontend (React 19)
* **React + Vite** — SPA rápida e reativa
* **TypeScript** — tipagem estática
* **React Router** — navegação
* **React Hook Form + Zod** — formulários e validação
* **Axios** — cliente HTTP

---

## Testes e Qualidade

O projeto tem uma suíte de testes automatizados que roda em **integração contínua a cada push e pull request**:

| Suíte | Tecnologia | Cobertura |
|---|---|---|
| **MyFinance.Domain.Tests** | xUnit | Entidades e regras de negócio (invariantes, validações) |
| **MyFinance.Application.Tests** | xUnit + Moq | Todos os Services, com repositórios mockados |
| **AiAgent** | pytest | Funções puras, tools financeiras, parsers e configuração |

O pipeline de **GitHub Actions** (`.github/workflows/ci.yml`) executa os testes .NET e Python em jobs paralelos. A branch `main` é protegida: **nenhum código é mesclado sem que todos os testes passem.**

```bash
# Rodar os testes .NET
dotnet test

# Rodar os testes do AiAgent
cd MyFinance.AiAgent && pytest
```

---

## Como Executar

### Pré-requisitos
* [.NET SDK 9.0](https://dotnet.microsoft.com/download)
* [Node.js](https://nodejs.org/) (18+)
* [Python 3.11](https://www.python.org/)
* [PostgreSQL](https://www.postgresql.org/)
* [Ollama](https://ollama.com/) *(opcional — necessário apenas para as funcionalidades de IA)*

### 1. Clonar o repositório
```bash
git clone https://github.com/FilipeTerra/MyFinance.git
cd MyFinance
```

### 2. Backend (API .NET)
Configure a connection string e as `JwtSettings` em `MyFinance.Api/appsettings.Development.json`, então:
```bash
cd MyFinance.Api
dotnet restore
dotnet ef database update   # aplica as migrations
dotnet run                  # sobe em http://localhost:5088
```

### 3. AiAgent (Python) — opcional
```bash
cd MyFinance.AiAgent
python -m venv venv && source venv/bin/activate
pip install -r requirements.txt
cp .env.example .env        # preencha as chaves conforme necessário
uvicorn src.Api.main:app --port 8181
```

### 4. Frontend (React)
As URLs da API são configuradas via variáveis de ambiente (`VITE_API_BASE_URL`, `VITE_AI_API_BASE_URL`) — veja `.env.development`.
```bash
cd MyFinance.Frontend
npm install
npm run dev                 # sobe em http://localhost:5173
```

---

## Roadmap

* [ ] Dashboard com gráficos interativos de tendências e categorização de gastos
* [ ] Incrementar as tools do agente ReAct
* [ ] Desenvolver perfil do usuário

---

Desenvolvido por **Filipe Caldeira** — Software Engineer 👨‍💻
