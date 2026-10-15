# Ingestão de PDFs

Documentação do pipeline **`POST /api/v1/ingest`** na **Api.Conhecimento** (`src/Api.Conhecimento/Ingest`), que adiciona conhecimento corporativo a partir de arquivos PDF para uso na busca RAG (incluindo a tool MCP **`BuscarConhecimento`**).

## Processamento

Fluxo implementado em **`IngestService.IngerirAsync`**:

1. **Validação** — usuário, código do documento, arquivo presente, tamanho máximo, extensão/content-type PDF e assinatura `%PDF` no cabeçalho.
2. **Extração de texto** — **`PdfSharpTextExtractor`** (PdfSharpCore) percorre páginas e operadores de texto (`Tj`/`TJ`).
3. **Chunking** — **`MarkdownChunker.Dividir`** (mesmo algoritmo usado para `.md` no startup): blocos por parágrafos, tamanho máximo e sobreposição configuráveis em **`RagOptions`**.
4. **Embeddings** — **`OpenAiEmbeddingService.GerarLoteAsync`** para todos os trechos (modelo `OpenAI:EmbeddingModel`, padrão `text-embedding-3-small`).
5. **Persistência Postgres** — **`PostgresChunkRepository.SubstituirDocumentoAsync`**: remove chunks anteriores do mesmo `codigo_documento` e insere novos registros na tabela **`chunks`** (pgvector), com metadados (`id_agente`, nome do arquivo, validade, usuário).
6. **Índice em memória** — **`InMemoryVectorStore.Adicionar`** atualiza o store usado quando não há chunks vigentes no Postgres ou como complemento local após ingest.

A resposta HTTP inclui **`mensagem`** e lista **`passos`** descrevendo cada etapa (útil para diagnóstico).

### Relação com outros caminhos de conhecimento

| Origem | Quando roda | Onde fica indexado |
|--------|-------------|-------------------|
| PDF via `/api/v1/ingest` | Sob demanda (REST) | Postgres + memória |
| Markdown em `Documentos/` | Startup (`DocumentIngestionHostedService`) | Apenas memória |

Na **busca** (`ConhecimentoSearchService.BuscarAsync`): se existirem chunks **vigentes** no Postgres (`ListarVigentesAsync`), a busca usa esses embeddings; caso contrário, usa somente o índice em memória (documentos `.md` do startup).

Busca: embeddings da consulta e de siglas detectadas (ex.: IBS, CBS) → similaridade de cosseno (máximo entre consultas) + boost literal quando o texto do chunk contém siglas/termos → top **`Rag:QuantidadeResultados`** (padrão 8). Logs Information em `ConhecimentoSearchService` listam scores do top-K.

## Configuração

### `appsettings.json` (Api.Conhecimento)

| Seção / chave | Função |
|---------------|--------|
| `ConnectionStrings:Rag` | Postgres + pgvector (`bd-rag-infis`, porta host `5433`) |
| `OpenAI:ApiKey` | Embeddings (`appsettings.Local.json`, User Secrets ou `OPENAI_API_KEY`; não versionar) |
| `OpenAI:EmbeddingModel` | Modelo de embedding |
| `Rag:PastaDocumentos` | Pasta relativa de `.md` no startup |
| `Rag:TamanhoMaximoChunk` | Tamanho alvo do chunk (caracteres) |
| `Rag:SobreposicaoChunk` | Sobreposição entre chunks |
| `Rag:QuantidadeResultados` | Top-K na busca |
| `Rag:IdAgente` | Identificador gravado nos chunks |
| `Rag:TamanhoMaximoArquivoBytes` | Limite do PDF (padrão 2 MB) |

### Docker

Scripts em `docker/postgres/init/` criam extensão **vector** e tabela **`chunks`**. Subir com:

```powershell
docker compose up -d
```

### Request multipart

| Campo | Obrigatório | Descrição |
|-------|-------------|-----------|
| `usuario` | Sim | Quem realizou a ingestão |
| `codigoDocumento` | Sim | Identificador lógico; substituição remove chunks anteriores com o mesmo código |
| `validade` | Não | `DateOnly`; usado na consulta de chunks vigentes |
| `arquivo` | Sim | PDF (máx. 2 MB; limite também no `[RequestSizeLimit]` do controller) |

Porta HTTP padrão: **5200**. Swagger: http://localhost:5200/swagger

## Execução e testes

### Executar a API

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "sua-chave" --project src/Api.Conhecimento
dotnet run --project src/Api.Conhecimento --launch-profile http
```

### Teste manual (REST Client)

Exemplo em `src/Api.Conhecimento/Api.Conhecimento.http`: multipart com `usuario`, `codigoDocumento`, `validade` e `arquivo` apontando para um PDF local.

**Sucesso (201):** corpo com mensagem *"Conhecimento cadastrado com sucesso"* e passos (validação → extração → chunks → embeddings → Postgres → memória).

**Falhas comuns (400):** PDF vazio ou sem texto extraível, arquivo inválido, campos obrigatórios ausentes, arquivo acima de 2 MB.

### Validar efeito na busca

1. Após ingest bem-sucedida, chamar MCP **`BuscarConhecimento`** (via Api.Atendimento `POST /conhecimento/buscar` ou cliente MCP em `/mcp`).
2. Confirmar trechos com `Fonte: <nome do arquivo.pdf>` alinhados ao conteúdo do PDF.
3. Opcional: inspecionar linhas em **`chunks`** no Postgres (PgAdmin em http://localhost:5051, host interno `postgres`).

### Testes automatizados

O projeto **`Api.Conhecimento.Tests`** contém testes unitários de **`SimilaridadeCosseno`** (base da ordenação dos resultados). Não há testes automatizados dedicados ao fluxo completo de ingest de PDF (extração, repositório, endpoint) — item pendente em `docs/checklist.md`.

```powershell
dotnet test src/CorporativoAI.sln
```
