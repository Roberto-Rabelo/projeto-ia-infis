# CorporativoAI

Solução corporativa em ASP.NET Core com duas APIs que se comunicam pelo Model Context Protocol (MCP). A `Api.Atendimento` orquestra a pergunta, consulta o conhecimento via MCP e responde com OpenAI em formato estruturado. A `Api.Conhecimento` é o MCP Server com RAG (chunking, embeddings e busca por similaridade de cosseno em memória).

## Arquitetura

```
Cliente
  → Api.Atendimento (REST)
  → MCP Client
  → Api.Conhecimento (/mcp)
  → embeddings + busca vetorial
  → Api.Atendimento
  → OpenAI (Structured Output)
  → resposta
```

| Projeto | Pasta | Papel | Porta HTTP |
|---|---|---|---|
| Api.Atendimento | `src/Api.Atendimento` | Orquestradora, MCP Client, OpenAI | `5100` |
| Api.Conhecimento | `src/Api.Conhecimento` | MCP Server, RAG em memória | `5200` |

Solution: `src/CorporativoAI.sln`

## Pré-requisitos

- .NET 8 SDK
- Chave da OpenAI (embeddings e chat)

## Configuração da chave

Não versionamos a chave no repositório. Use User Secrets ou variável de ambiente.

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "sua-chave" --project src/Api.Conhecimento
dotnet user-secrets set "OpenAI:ApiKey" "sua-chave" --project src/Api.Atendimento
```

Ou defina `OPENAI_API_KEY`.

Modelos padrão:

- Chat: `gpt-4o-mini`
- Embeddings: `text-embedding-3-small`

## Como executar

Suba as duas APIs. A de conhecimento precisa estar no ar antes do atendimento.

```powershell
dotnet run --project src/Api.Conhecimento --launch-profile http
dotnet run --project src/Api.Atendimento --launch-profile http
```

- Swagger Atendimento: http://localhost:5100/swagger
- Swagger Conhecimento: http://localhost:5200/swagger
- MCP: http://localhost:5200/mcp

## Endpoints principais

### Atendimento

`POST /atendimento`

```json
{ "pergunta": "Qual o horário de atendimento ao aluno?" }
```

Resposta estruturada: `resposta`, `categoria`, `fontes`, `modelo`, `fallback`, `tokensEntrada`, `tokensSaida`, `custoEstimado`.

Também existem `GET /conhecimento/ferramentas` e `POST /conhecimento/buscar` para validar o MCP.

### Conhecimento

- `GET /health`
- `POST /mcp` — tools MCP, incluindo `BuscarConhecimento`

Os documentos `.md` ficam em `src/Api.Conhecimento/Documentos`. No startup eles são lidos, divididos em chunks, embeddados e indexados em memória (sem banco).

## Testes

```powershell
dotnet test src/CorporativoAI.sln
```

Há testes unitários da similaridade de cosseno em `src/Api.Conhecimento.Tests`.

## Documentação do projeto

- `docs/requirements.md`
- `docs/architecture.md`
- `docs/spectifications.md`
- `docs/task.md`
