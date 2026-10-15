# Checklist do projeto CorporativoAI



> **Manutenção:** ao implementar, alterar ou remover funcionalidades neste repositório, **atualize este arquivo** na mesma entrega: marque itens concluídos, inclua novos pendentes e ajuste a data em **Última revisão**. Referência de escopo: `docs/requirements.md`, `docs/task.md`.



**Última revisão:** 2026-10-13 (Etapa 9 — RabbitMQ, relay outbox, worker, Mailhog, InboxMessage)



Legenda: `[x]` implementado · `[ ]` pendente · `[-]` parcial



---



## Infraestrutura e solution



- [x] Solution `CorporativoAI.sln` com Api.Atendimento e Api.Conhecimento

- [x] Docker Compose (Postgres/pgvector, MongoDB, SQL Server, UIs)

- [x] Docker RabbitMQ (`mensageria-infis-rabbitmq`, fila `atendimentos`, management `:15672`)

- [x] Docker Mailhog (`notificacao-infis-mailhog`, SMTP `:1025`, UI `:8025`)

- [x] Projeto **`Worker.AtendimentoSuporte`** na solution

- [x] Scripts de init (chunks, conversas, ConsumoTokens, OutboxMessage, InboxMessage)

- [x] Swagger em ambas as APIs

- [x] Health checks (`GET /health`)

- [x] README operacional na raiz



## Api.Conhecimento — MCP e RAG



- [x] MCP Server (`/mcp`) com tool **`BuscarConhecimento`**

- [x] Carregamento de documentos `.md` na pasta `Documentos/` no startup

- [x] Chunking configurável (`MarkdownChunker`)

- [x] Embeddings OpenAI (`OpenAiEmbeddingService`)

- [x] Índice vetorial em memória (`InMemoryVectorStore`)

- [x] Persistência de chunks PDF no Postgres + pgvector

- [x] Busca por similaridade de cosseno com top-K configurável (padrão 8)

- [x] Busca híbrida: multi-embedding (consulta + siglas) e boost literal (`ConsultaRagTermos`)

- [x] Logs de score no top-K da busca RAG (`ConhecimentoSearchService`)

- [x] Prioridade de chunks vigentes no Postgres sobre memória na busca

- [x] **`POST /api/v1/ingest`** — ingestão de PDF (validação, extração, embeddings, persistência)

- [x] `GET/PUT /api/v1/config/openai` (Development) — consultar e atualizar chave OpenAI (embeddings)



## Api.Atendimento — orquestração



- [x] **`POST /api/v1/atendimento`** (REST, validação de entrada)

- [x] MCP Client para Api.Conhecimento

- [x] Endpoints de diagnóstico MCP (`/conhecimento/ferramentas`, `/conhecimento/buscar`)

- [x] Gestão da chave OpenAI em Development (`GET/PUT /api/v1/config/openai`, máscara + persistência em `appsettings.Local.json`)

- [x] Gestão da chave Gemini em Development (`GET/PUT /api/v1/config/gemini`, `Gemini:ApiKey` / `GEMINI_API_KEY` no user-secrets)

- [x] Flag `encaminharSuporteHumano` na saída estruturada e na resposta REST

- [x] Prompt identifica pedido de humano ou insatisfação (`PromptAtendimento`)

- [x] Encaminhamento via Semantic Kernel (`EncaminhamentoSuportePlugin` / `encaminhar_atendimento_humano`)

- [x] Outbox SQL `dbo.OutboxMessage` (`SqlOutboxMessageRepository`, scripts em `docker/sqlserver/init/`)

- [x] Relay outbox → RabbitMQ (`OutboxRelayBackgroundService`, fila `atendimentos`, lote 5 / 60 s)

- [x] Integração OpenAI Chat como LLM principal

- [x] Structured Output (JSON: resposta, categoria, fontes)

- [x] Prompt de domínio tributário/contábil (`PromptAtendimento`, resposta parcial quando o tema aparece no contexto)

- [x] Fallback **Gemini** quando OpenAI falhar (`GeminiFallbackGerador`, SDK `Google.GenAI`, modelo `gemini-3.5-flash-lite`)

- [x] Uso real do campo `fallback: true` na resposta quando a contingência for acionada



## Worker.AtendimentoSuporte — notificação



- [x] Consumo da fila RabbitMQ `atendimentos` (`AtendimentoFilaConsumerHostedService`)

- [x] Persistência `dbo.InboxMessage` (`SqlInboxMessageRepository`)

- [x] E-mail HTML via Mailhog (`MailKitAtendimentoSuporteEmailSender`, remetente/destinatário conforme `task.md`)



## Persistência e custos



- [x] Histórico conversacional no MongoDB (por usuário, N últimas conversas no prompt)

- [x] Registro de consumo de tokens no SQL Server

- [x] Cálculo de custo estimado (preços por milhão configuráveis)

- [x] Vínculo `IdConversa` entre custo SQL e documento Mongo



## Qualidade e documentação



- [-] Testes automatizados — `SimilaridadeCossenoTests`, `ConsultaRagTermosTests`; faltam testes de ingest, MCP, atendimento, outbox/worker e integração

- [-] Tratamento de erros — ProblemDetails registrado; revisão ampla de cenários de falha (Etapa 10) pendente

- [-] Documentação — `docs/requirements.md`, `architecture.md`, `spectifications.md`, `task.md`, README; `atendimento.md`, `ingest.md`, `checklist.md`, **`mensageria-suporte.md`** (Etapa 9)

- [ ] Revisão final de código (Etapa 10 em `docs/task.md`)



## Requisitos não funcionais (`docs/requirements.md`)



- [x] ASP.NET Core, async/await, DI, CancellationToken, logging

- [x] API Keys fora do código (`appsettings.Local.json` gitignored, User Secrets, env)

- [-] Testes automatizados (cobertura limitada)

- [-] Tratamento de erros (básico presente; ampliar conforme necessidade)



---



## Próximos passos sugeridos (ordem do `docs/task.md`)



1. [x] Implementar Gemini e fallback (`OpenAiRespostaGerador` + `GeminiFallbackGerador` em `OpenAiAtendimentoService`)

2. [x] Etapa 9 — mensageria outbox/inbox e e-mail (ver `docs/mensageria-suporte.md`)

3. [ ] Expandir suite de testes (ingest, RAG, MCP client, atendimento, relay/worker)

4. [ ] Revisão de código e hardening de erros observabilidade (Etapa 10)

