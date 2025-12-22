# MyFinance

> **Sistema de gerenciamento financeiro pessoal, com análises personalizadas e detalhadas.**

## Sobre o Projeto

O **MyFinance** nasceu da união de dois objetivos pessoais: a necessidade de manter as finanças organizadas e o desejo constante de evoluir tecnicamente.

Este projeto não é apenas uma ferramenta para controle de gastos, mas também um laboratório de boas práticas de desenvolvimento. Aqui, aplico conceitos de arquitetura de software, design patterns e tecnologias modernas para criar uma solução robusta e escalável, resolvendo um problema real do meu dia a dia.

## Funcionalidades

O sistema foi desenhado para ser prático e intuitivo:

* **Home (Visão Geral):** Focada na agilidade. Permite o registro rápido de transações (receitas e despesas), consulta de histórico e visualização resumida das contas cadastradas.
* **Gestão de Contas e Transações:** CRUD completo para manter seus dados sempre atualizados.
* **Dashboard (Em Desenvolvimento):** Uma área dedicada à inteligência financeira, com gráficos de tendências, categorização de gastos e análises detalhadas para auxiliar na tomada de decisão.

## Tecnologias Utilizadas

O projeto utiliza uma stack moderna, separada entre Backend (API) e Frontend (SPA):

### Backend (.NET Core)
* **C# / .NET:** Construção da API RESTful.
* **Entity Framework Core:** ORM para interação com o banco de dados.
* **Arquitetura:** O projeto segue princípios de **Clean Architecture** (separação em Api, Application, Domain, Infrastructure) para garantir desacoplamento e testabilidade.
* **Boas Práticas:** Injeção de dependência, DTOs, Repository Pattern.

### Frontend (React)
* **React + Vite:** Para uma interface rápida e reativa.
* **TypeScript:** Garantindo tipagem estática e segurança no desenvolvimento.
* **CSS Modules/Custom CSS:** Estilização componentizada.

## ⚙️ Como Executar

### Pré-requisitos
* .NET SDK.
* Node.js.
* Postgres (ou configurar a connection string para seu banco de preferência).

### Passos

1.  **Clone o repositório:**
    ```bash
    git clone [https://github.com/seu-usuario/myfinance.git](https://github.com/seu-usuario/myfinance.git)
    ```

2.  **Backend:**
    ```bash
    cd MyFinance.Api
    dotnet restore
    dotnet run
    ```

3.  **Frontend:**
    ```bash
    cd MyFinance.Frontend
    npm install
    npm run dev
    ```

## Em desenvolvimento

* [ ] Implementação da aba Dashboard com gráficos interativos.
* [ ] Relatórios detalhados sobre gastos, receitas e previsões.
* [ ] Metas de economia e orçamentos.

---
Desenvolvido por **[Seu Nome]** 👨‍💻
