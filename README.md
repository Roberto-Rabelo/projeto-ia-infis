# CorporativoAI

Solução corporativa em ASP.NET Core com duas APIs que se comunicam pelo **Model Context Protocol (MCP)**. A **Api.Atendimento** orquestra perguntas tributárias e contábeis, consulta conhecimento via MCP, usa histórico conversacional e registra custos de tokens. A **Api.Conhecimento** expõe o MCP Server com **RAG** (embeddings, similaridade de cosseno, Postgres/pgvector e ingestão de PDF).

## Arquitetura

```
Cliente
  → Api.Atendimento (REST)
       ├─ MongoDB (histórico por usuário)
       ├─ SQL Server (consumo de tokens)
       ├─ MCP Client → Api.Conhecimento (/mcp)
       └─ OpenAI Chat (resposta estruturada)
  → Api.Conhecimento
       ├─ RAG (Postgres vigente ou índice em memória)
       └─ OpenAI Embeddings
```

| Projeto | Pasta | Papel | Porta HTTP |
|---|---|---|---|
| Api.Atendimento | `src/Api.Atendimento` | Orquestradora, MCP Client, histórico, custos | `5100` |
| Api.Conhecimento | `src/Api.Conhecimento` | MCP Server, RAG, ingest PDF | `5200` |
| Worker.AtendimentoSuporte | `src/Worker.AtendimentoSuporte` | Fila RabbitMQ → Inbox SQL + e-mail Mailhog | — |

Solution: `src/CorporativoAI.sln`

## Pré-requisitos

- .NET 8 SDK
- Docker Desktop
- Chave da OpenAI (embeddings e chat)

## Infraestrutura local (Docker)

Todos os serviços usam a rede `rede-rag-infis`.

```powershell
docker compose up -d
```

| Serviço | URL / porta | Uso | Credenciais |
|---|---|---|---|
| PostgreSQL + pgvector | `localhost:5433` | Chunks RAG | banco `bd-rag-infis`, `infis` / `infis` |
| PgAdmin | http://localhost:5051 | GUI Postgres | `infis@email.com` / `infis@2026` |
| MongoDB | `localhost:27018` | Histórico de atendimento | banco `bd-historico-atendimentos`, `infis` / `infis` |
| Mongo Express | http://localhost:5052 | GUI Mongo | `infis` / `infis` |
| SQL Server | `localhost:1434` | Custos de tokens | banco `bd-custos-ia`, `sa` / `Coti@2026` |
| Adminer | http://localhost:5053 | GUI SQL Server | login `infis` / `infis`; sistema **MS SQL**; servidor `sqlserver` |
| RabbitMQ | `localhost:5672`, UI http://localhost:15672 | Fila `atendimentos` (Etapa 9) | `infis` / `infis` |
| Mailhog | SMTP `localhost:1025`, UI http://localhost:8025 | E-mail de encaminhamento humano | — |

No PgAdmin, o host interno é `postgres` (porta 5432). O servidor `bd-rag-infis` já vem pré-cadastrado em `docker/pgadmin/servers.json`.

Scripts de inicialização:

- `docker/postgres/init/` — extensão `vector`, tabela `chunks`
- `docker/mongodb/init/` — collection `conversas` e índice por usuário/data
- `docker/sqlserver/init/` — banco `bd-custos-ia`, tabelas `ConsumoTokens`, `OutboxMessage` (Etapa 8), `InboxMessage` (Etapa 9)

```powershell
docker compose down
```

Para recriar dados do zero: `docker compose down -v`.

## Configuração da chave OpenAI

Não versionamos chaves no repositório. Escolha **uma** das formas abaixo (prioridade: `appsettings.Local.json` > User Secrets / appsettings > variável de ambiente).

### Opção 1 — Arquivo local (recomendado para editar no IDE)

Em cada API, copie o exemplo e preencha a chave:

```powershell
Copy-Item src/Api.Atendimento/appsettings.Local.example.json src/Api.Atendimento/appsettings.Local.json
Copy-Item src/Api.Conhecimento/appsettings.Local.example.json src/Api.Conhecimento/appsettings.Local.json
```

Edite `OpenAI:ApiKey` em:

- `src/Api.Atendimento/appsettings.Local.json` — chat do atendimento
- `src/Api.Conhecimento/appsettings.Local.json` — embeddings do RAG

O arquivo `appsettings.Local.json` está no `.gitignore` e **não** vai para o Git. Reinicie a API após alterar a chave manualmente no disco (a API REST abaixo aplica sem reiniciar).

### Opção 2 — API de gestão (Development, Swagger)

Com `ASPNETCORE_ENVIRONMENT=Development`, cada API expõe:

| Método | Rota | Função |
|--------|------|--------|
| `GET` | `/api/v1/config/openai` | Status: origem, sufixo, máscara |
| `GET` | `/api/v1/config/openai?completa=true` | Inclui `apiKeyCompleta` e `caminhoUserSecrets` (Development) |
| `PUT` | `/api/v1/config/openai` | Corpo `{ "apiKey": "sk-...", "persistirEm": "UserSecrets" }` — valores: `UserSecrets` (padrão), `Local`, `Ambos` |
| `GET` | `/api/v1/config/gemini?completa=true` | Status da chave Gemini (fallback) |
| `PUT` | `/api/v1/config/gemini` | Corpo `{ "apiKey": "...", "persistirEm": "UserSecrets" }` — grava `Gemini:ApiKey` e `GEMINI_API_KEY` no user-secrets |

Repita o `PUT` OpenAI em **Api.Atendimento (5100)** e **Api.Conhecimento (5200)** se usar chaves distintas; em geral use a mesma chave nos dois. Gemini só existe na **Api.Atendimento**.

Em Production esses endpoints retornam `404`.

### Opção 3 — User Secrets (o que você já usa)

Cada projeto tem seu **UserSecretsId** no `.csproj`. A chave fica em um JSON **fora do repositório**:

| Projeto | Arquivo no Windows |
|---------|-------------------|
| Api.Atendimento | `%APPDATA%\Microsoft\UserSecrets\07f79b64-25c9-4d53-86f8-2172de4fbfa3\secrets.json` |
| Api.Conhecimento | `%APPDATA%\Microsoft\UserSecrets\corporativoai-api-conhecimento\secrets.json` |

Conteúdo típico:

```json
{
  "OpenAI:ApiKey": "sk-..."
}
```

**CLI (PowerShell, na raiz do repo):**

```powershell
# Ver a chave completa (e outras secrets)
dotnet user-secrets list --project src/Api.Atendimento
dotnet user-secrets list --project src/Api.Conhecimento

# Trocar a chave
dotnet user-secrets set "OpenAI:ApiKey" "sk-nova-chave" --project src/Api.Atendimento
dotnet user-secrets set "OpenAI:ApiKey" "sk-nova-chave" --project src/Api.Conhecimento

# Remover só a chave OpenAI
dotnet user-secrets remove "OpenAI:ApiKey" --project src/Api.Atendimento
```

**Visual Studio:** clique com o botão direito no projeto → **Gerenciar Segredos do Usuário** → edite o JSON.

**Swagger / API (Development):** `GET /api/v1/config/openai?completa=true` devolve `apiKeyCompleta` e `caminhoUserSecrets`. `PUT` com `"persistirEm": "UserSecrets"` (padrão) grava direto no mesmo `secrets.json` **sem reiniciar** a API.

Gemini (fallback): `dotnet user-secrets set "Gemini:ApiKey" "..." --project src/Api.Atendimento`

### Opção 4 — Variável de ambiente

Defina `OPENAI_API_KEY` (Windows: Painel de controle ou `$env:OPENAI_API_KEY = "..."` no PowerShell antes do `dotnet run`). Fallback Gemini: `GEMINI_API_KEY` ou `GOOGLE_API_KEY`.

No log de startup aparece `Chave OpenAI carregada de ... sufixo XXXX` (últimos 4 caracteres) para confirmar qual origem foi usada.

Modelos padrão:

- Chat: `gpt-4o-mini`
- Embeddings: `text-embedding-3-small`

Conexões das APIs (valores padrão em `appsettings.json`):

- **Api.Conhecimento:** `ConnectionStrings:Rag` → Postgres local
- **Api.Atendimento:** `MongoHistorico`, `SqlCustos`, `Mcp:Conhecimento:Endpoint`

## Como executar

1. Suba o Docker: `docker compose up -d`
2. Inicie a Api.Conhecimento (precisa estar no ar antes do atendimento)
3. Inicie a Api.Atendimento

```powershell
dotnet run --project src/Api.Conhecimento --launch-profile http
dotnet run --project src/Api.Atendimento --launch-profile http
```

- Swagger Atendimento: http://localhost:5100/swagger
- Swagger Conhecimento: http://localhost:5200/swagger
- MCP: http://localhost:5200/mcp

Arquivos `.http` em cada projeto trazem exemplos de chamada.

## Endpoints principais

### Api.Atendimento

**`POST /api/v1/atendimento`** — assistente tributário/contábil.

Request:

```json
{
  "usuario": "Ana Souza",
  "pergunta": "Qual o prazo para entrega da DCTF?"
}
```

Response: `resposta`, `categoria`, `fontes`, `modelo`, `fallback`, `tokensEntrada`, `tokensSaida`, `custoEstimado`, `encaminharSuporteHumano`, `idOutboxEncaminhamento` (quando aplicável).

Fluxo interno:

1. Recupera as **3 últimas** conversas do usuário no MongoDB e inclui no prompt.
2. Busca contexto RAG via MCP (`BuscarConhecimento` na Api.Conhecimento).
3. Gera resposta estruturada com OpenAI; em falha, fallback com Gemini (`gemini-3.5-flash-lite`, `fallback: true`).
4. Se o modelo indicar encaminhamento humano, grava evento na outbox SQL via plugin Semantic Kernel (`OutboxMessage`).
5. Em background, relay publica pendências na fila RabbitMQ `atendimentos` (ver `docs/mensageria-suporte.md`).
6. Grava a interação no MongoDB (`conversas`).
7. Grava consumo de tokens no SQL Server (`ConsumoTokens`), com `IdConversa` referenciando o documento Mongo.

Com **`Worker.AtendimentoSuporte`** em execução, mensagens da fila geram registro em `InboxMessage` e e-mail no Mailhog.

Outros endpoints úteis:

- `GET /health`
- `GET /conhecimento/ferramentas` e `POST /conhecimento/buscar` — validação do MCP

### Api.Conhecimento

- `GET /health`
- `POST /mcp` — tools MCP, incluindo `BuscarConhecimento`
- **`POST /api/v1/ingest`** — PDF (máx. 2 MB), multipart: `usuario`, `codigoDocumento`, `validade`, `arquivo`

RAG:

- Documentos `.md` em `src/Api.Conhecimento/Documentos` são indexados em memória no startup.
- PDFs ingeridos vão para Postgres (chunks + embeddings) e também para o índice em memória.
- A busca MCP prioriza chunks **vigentes** no Postgres; se vazio, usa só memória.

## Persistência

| Dado | Tecnologia | Collection / tabela |
|---|---|---|
| Histórico conversacional | MongoDB | `conversas` |
| Consumo de tokens | SQL Server | `dbo.ConsumoTokens` |
| Base RAG (PDF) | PostgreSQL + pgvector | `chunks` |

Campos principais do histórico Mongo: `Id`, `dataHora`, `usuario`, `agente`, `pergunta`, `respostaEstruturada`, `respostaTexto`.

Campos principais de custos SQL: tokens entrada/saída, custos parciais, `custoTotal`, `IdConversa`.

## Testes

```powershell
dotnet test src/CorporativoAI.sln
```

Testes unitários de similaridade de cosseno em `src/Api.Conhecimento.Tests`.

## Documentação do projeto

- `docs/requirements.md`
- `docs/architecture.md`
- `docs/spectifications.md`
- `docs/task.md`
