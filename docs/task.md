# Tasks

## Etapa 1

- criar solution
- criar Api.Atendimento
- criar Api.Conhecimento
- configurar Swagger

## Etapa 2

- transformar Api.Conhecimento em MCP Server
- criar tool BuscarConhecimento

## Etapa 3

- criar MCP Client na Api.Atendimento
- testar descoberta das tools
- testar chamada MCP

## Etapa 4

- implementar serviço de embeddings
- implementar chunking
- implementar armazenamento vetorial
- implementar busca por similaridade

## Etapa 5

- integrar OpenAI
- implementar Structured Output

## Etapa 6

- registrar tokens
- calcular custo

## Etapa 7

Implemente a etapa 7 do arquivo tasks.md
Para integração com o gemini use o modelo: gemini-3.5-flash-lite
Atualize o checklist após a implementação


## Etapa 8

Implementado (2026-10-13): ver `docs/atendimento.md` — seção **Suporte humano e outbox**.

- Criar uma flag na saída estruturada que indique se o atendimento deve ser encaminhado para um suporte humano
- Modificar o prompt para identificar a necessidade anterior sob 2 condições: 
  ou o usuário pede explicitamente para ser atendimento por um humano ou o agente identifica 
  que o usuário está insatisfeito com o atendimento.
- Crie um função usando Kernel Semantico para que o agente possa executar o encaminhamento do atendimento
  Essa função deverá apenas gravar um registro em uma OutboxMessage no SqlServer contendo:
	Id sequencial
	Data e hora de gravação
	Transmitido (default false)
	Data e hora da transmissão (default null)
	Nome da fila (default null)
	Usuário
	Descrição (conteudo gerado pela IA com o resumo do atendimento)

## Etapa 9

Implementado (2026-10-13): ver [`docs/mensageria-suporte.md`](mensageria-suporte.md).

- RabbitMQ e Mailhog no `docker-compose.yml`
- `OutboxRelayBackgroundService` + `RabbitMqOutboxAtendimentoPublisher` na Api.Atendimento (lote 5, intervalo 60 s, fila `atendimentos`)
- Projeto **`Worker.AtendimentoSuporte`** — consumo da fila, `dbo.InboxMessage`, e-mail Mailhog (HTML)
- Script SQL `03-inbox-message.sql` / `InboxMessage` em `01-create-database.sql`


## Etapa 10

- revisar código