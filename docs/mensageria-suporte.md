# Mensageria e notificação — Etapa 9

Fluxo assíncrono após o encaminhamento humano (Etapa 8): **Outbox SQL → RabbitMQ → Worker → Inbox SQL + e-mail (Mailhog)**.

## Componentes

| Peça | Onde | Função |
|------|------|--------|
| `OutboxRelayBackgroundService` | Api.Atendimento | A cada **60 s**, lê até **5** linhas com `Transmitido = 0`, publica JSON na fila **`atendimentos`** e marca a outbox |
| RabbitMQ | Docker `mensageria-infis-rabbitmq` | Fila durável `atendimentos` |
| `Worker.AtendimentoSuporte` | `src/Worker.AtendimentoSuporte` | Consome a fila, grava **`InboxMessage`**, envia e-mail via SMTP |
| Mailhog | Docker `notificacao-infis-mailhog` | SMTP `localhost:1025`, UI http://localhost:8025 |

## Payload na fila (JSON)

```json
{
  "id": 1,
  "usuario": "Ana Souza",
  "descricao": "Resumo gerado pela IA..."
}
```

`MessageId` do RabbitMQ = `id` da outbox (string).

## Tabelas SQL (`bd-custos-ia`)

- **`OutboxMessage`** — origem (Etapa 8); após relay: `Transmitido = 1`, `NomeFila = 'atendimentos'`, `DataHoraTransmissao` preenchida.
- **`InboxMessage`** — histórico no worker: `MensagemFilaId`, `DataHora`, `NomeFila`, `Usuario`, `Descricao`, `StatusEnvioEmail` (`Enviado` ou `Falha: ...`).

Scripts: `docker/sqlserver/init/02-outbox-message.sql`, `03-inbox-message.sql` (também embutidos em `01-create-database.sql`).

## Configuração

**Api.Atendimento** — seção `OutboxRelay` em `appsettings.json` (host `localhost:5672`, usuário/senha `infis`/`infis`). Desligue com `"Habilitado": false`.

**Worker** — `RabbitMq`, `SmtpNotificacao` (Mailhog `localhost:1025`), `SqlInbox` (mesma connection string de custos).

## E-mail (Mailhog)

- **De:** `worker-service@infis.com.br`
- **Para:** `atendimento-suporte@infis.com.br`
- **Assunto:** `Novo atendimento solicitado por: {usuario}`
- Corpo HTML padrão com resumo (`Descricao`).

## Como subir e testar

1. `docker compose up -d` (inclui RabbitMQ e Mailhog).
2. Se o SQL já existia: `docker exec custos-infis-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Coti@2026" -C -i /init/03-inbox-message.sql`
3. Api.Atendimento (porta 5100) + Worker:

```powershell
dotnet run --project src/Worker.AtendimentoSuporte
```

4. `POST /api/v1/atendimento` pedindo atendimento humano.
5. Aguarde até **1 minuto** (relay) ou reinicie a API para forçar ciclo.
6. Verifique:
   - Outbox: `Transmitido = 1`
   - Inbox: nova linha em `dbo.InboxMessage`
   - Mailhog: http://localhost:8025

## RabbitMQ Management

http://localhost:15672 — usuário `infis`, senha `infis`. Fila `atendimentos`.
