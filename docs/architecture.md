# Arquitetura

## Solução

CorporativoAI.sln

Projetos:

- Api.Atendimento
- Api.Conhecimento
- Worker.AtendimentoSuporte

## Api.Atendimento

Responsabilidades:

REST API
Orquestração
MCP Client
OpenAI
Gemini fallback
Structured Outputs
Controle de tokens
Controle de custos
Encaminhamento suporte humano (Semantic Kernel → Outbox SQL)
Relay outbox → RabbitMQ (BackgroundService)

## Worker.AtendimentoSuporte

Responsabilidades:

Consumo fila RabbitMQ `atendimentos`
Gravação InboxMessage (SQL)
Envio e-mail HTML (Mailhog / SMTP)

## Api.Conhecimento

Responsabilidades:

MCP Server
Ingestão de documentos
Chunking
Embeddings
Busca vetorial
RAG

## Fluxo

Cliente
→ Api.Atendimento
→ MCP Client
→ Api.Conhecimento
→ Embeddings
→ Busca vetorial
→ MCP
→ Api.Atendimento
→ OpenAI (ou Gemini fallback)
→ Se encaminharSuporteHumano: Kernel → OutboxMessage (SQL)
→ Resposta REST
→ (async) Outbox relay → RabbitMQ → Worker → InboxMessage + e-mail
