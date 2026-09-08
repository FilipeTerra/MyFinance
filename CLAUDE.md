# Diretrizes para o Claude neste repositório

MyFinance é um gestor de finanças pessoais com foco em IA: agentes analisam o comportamento financeiro do usuário, detectam vazamentos de dinheiro e atuam como consultores baseados em literatura financeira — não é "só um CRUD". Ao propor soluções, prefira simples e direto a mirabolante; evite overengineering.

**Stack:** API em C#/.NET (Clean Architecture) + EF Core + PostgreSQL · AiAgent em Python (FastAPI, LangChain/LangGraph, Ollama) · Frontend em React + TypeScript + Vite.

## Commits

**Nunca crie um commit sem que o usuário peça explicitamente**, mesmo depois de terminar uma tarefa, corrigir um bug ou concluir um plano aprovado. Terminar a implementação não é pedido de commit.

Deixe as mudanças no working tree (staged com `git add` quando fizer sentido, para o usuário revisar com `git diff --staged`) e informe o que foi feito. O usuário prefere revisar o que está em stage antes de decidir se quer commitar — só crie o commit quando ele disser algo como "pode commitar", "cria o commit" ou equivalente.

## Arquitetura (Clean Architecture)

- **Domain**: sem dependências externas. Só entidades com regra de negócio (`private set` em propriedades), value objects, enums e interfaces de repositório.
- **Application**: casos de uso (Services) e DTOs. Controller nunca tem regra de negócio — só orquestra a requisição e chama o Service.
- **Infrastructure**: implementação de repositórios (EF Core) e integrações externas.
- Sempre injeção de dependência e `async/await`. Sempre princípios SOLID.
- Mudança que envolve banco: comece pela Entidade de Domínio + Migration, antes de Controller/Service.

## AiAgent (Python)

- Orquestração via LangGraph; o LLM roteia e raciocina (padrão ReAct) — cálculo e acesso a dado rigoroso passam por Tools/MCP conectadas à API .NET. A IA nunca inventa número.
- Modelo pequeno e rápido (SLM) para processamento em background/auditoria; modelo robusto só nas interações de chat.
- Toda chamada à IA deve degradar sem quebrar o fluxo determinístico (ver `StatementImportService`) — a IA nunca é um passo obrigatório do caminho principal.

## Segurança e segredos

- Nunca commitar `.env`, credenciais, connection string ou token — sempre variável de ambiente ou `.env` ignorado pelo git.
- Validar toda entrada na fronteira (controller/API); nunca confiar em dado vindo do cliente sem checar (ex.: o `CategoryId` enviado pertence mesmo ao usuário autenticado?).
- Nunca expor exceção, stack trace ou mensagem de erro de banco ao cliente — logar o detalhe no servidor e responder mensagem genérica.

## Padrões de erro e resposta de API

- Operação em lote é tudo-ou-nada: valide o lote inteiro antes de gravar qualquer linha.
- Erro tem que ser acionável — aponte o quê (linha, campo) e o porquê, nunca só "falha ao salvar".
- Um item com problema não pode derrubar os outros de uma mesma operação (ver importação de extrato: um arquivo ilegível não impede os demais do lote).
- Frontend nunca usa `alert()` do navegador — usa o `FeedbackModal`/popup padrão do sistema.

## Performance e processamento assíncrono

- Nunca repita, por item de uma coleção, algo que só precisa acontecer uma vez por requisição (contexto do usuário, checagem de disponibilidade de um serviço externo).
- Só paralelize o que não compartilha o `ApplicationDbContext` da requisição (`Scoped`, não thread-safe): parsing e chamada HTTP externa podem rodar em paralelo; leitura/escrita no banco, não.

## Acessibilidade e responsividade (Frontend)

- Celular não rola de lado: tabela com muitas colunas vira cartão em CSS (ver `TransactionList.css`), com `role`/`aria-*` explícitos sempre que o CSS trocar o `display` de tabela para outra coisa.
- Alvo de toque mínimo de 44px; campo de formulário nunca abaixo de 16px de fonte — abaixo disso o Safari do iOS dá zoom ao focar.
- Popup/diálogo novo reaproveita o `Modal` compartilhado (foco preso, fecha no Escape, devolve o foco) em vez de reimplementar isso do zero.

## Padrões de código

- Código-fonte (nomes de variável, método, classe, tabela) sempre em **inglês**. Só mensagens de UI e comentários/documentação em **português**.
- Comente apenas o não-óbvio (uma decisão, uma restrição escondida) — nunca o que já é óbvio pelo nome. XML doc em todo membro público do C#.

## Testes

- Sempre crie testes para novas funcionalidades.
- Nunca exclua um teste.
- Nunca modifique um teste, **exceto** se a funcionalidade relacionada a ele mudou de comportamento. Exemplo: uma classe `Computador` tem a funcionalidade `Ligar`; antes ela ligava só a tela, agora liga tela e teclado — nesse caso o teste precisa mudar junto.
