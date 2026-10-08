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

No PgAdmin, o host interno é `postgres` (porta 5432). O servidor `bd-rag-infis` já vem pré-cadastrado em `docker/pgadmin/servers.json`.

Scripts de inicialização:

- `docker/postgres/init/` — extensão `vector`, tabela `chunks`
- `docker/mongodb/init/` — collection `conversas` e índice por usuário/data
- `docker/sqlserver/init/` — banco `bd-custos-ia`, tabela `ConsumoTokens`

```powershell
docker compose down
```

Para recriar dados do zero: `docker compose down -v`.

## Configuração da chave OpenAI

Não versionamos chaves no repositório. Use User Secrets ou variável de ambiente.

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "sua-chave" --project src/Api.Conhecimento
dotnet user-secrets set "OpenAI:ApiKey" "sua-chave" --project src/Api.Atendimento
```

Ou defina `OPENAI_API_KEY`.

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

Response: `resposta`, `categoria`, `fontes`, `modelo`, `fallback`, `tokensEntrada`, `tokensSaida`, `custoEstimado`.

Fluxo interno:

1. Recupera as **3 últimas** conversas do usuário no MongoDB e inclui no prompt.
2. Busca contexto RAG via MCP (`BuscarConhecimento` na Api.Conhecimento).
3. Gera resposta estruturada com OpenAI.
4. Grava a interação no MongoDB (`conversas`).
5. Grava consumo de tokens no SQL Server (`ConsumoTokens`), com `IdConversa` referenciando o documento Mongo.

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
