using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.AtendimentoSuporte.Email;
using Worker.AtendimentoSuporte.Inbox;
using Worker.AtendimentoSuporte.Messaging;
using Worker.AtendimentoSuporte.Options;

namespace Worker.AtendimentoSuporte;

public sealed class AtendimentoFilaConsumerHostedService : BackgroundService
{
    private readonly RabbitMqConsumerOptions _rabbitOptions;
    private readonly IInboxMessageRepository _inbox;
    private readonly IAtendimentoSuporteEmailSender _emailSender;
    private readonly ILogger<AtendimentoFilaConsumerHostedService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AtendimentoFilaConsumerHostedService(
        Microsoft.Extensions.Options.IOptions<RabbitMqConsumerOptions> rabbitOptions,
        IInboxMessageRepository inbox,
        IAtendimentoSuporteEmailSender emailSender,
        ILogger<AtendimentoFilaConsumerHostedService> logger)
    {
        _rabbitOptions = rabbitOptions.Value;
        _inbox = inbox;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitOptions.HostName,
            Port = _rabbitOptions.Porta,
            UserName = _rabbitOptions.Usuario,
            Password = _rabbitOptions.Senha,
            VirtualHost = _rabbitOptions.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(
            queue: _rabbitOptions.NomeFila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            await ProcessarMensagemAsync(args, stoppingToken);
        };

        _channel.BasicConsume(
            queue: _rabbitOptions.NomeFila,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Consumindo fila {Fila} em {Host}:{Porta}.",
            _rabbitOptions.NomeFila,
            _rabbitOptions.HostName,
            _rabbitOptions.Porta);

        stoppingToken.Register(() =>
        {
            _channel?.Close();
            _connection?.Close();
        });

        return Task.CompletedTask;
    }

    private async Task ProcessarMensagemAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        var mensagemFilaId = args.BasicProperties.MessageId
            ?? args.DeliveryTag.ToString();

        string statusEmail;
        OutboxAtendimentoMensagem? payload = null;

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            payload = JsonSerializer.Deserialize<OutboxAtendimentoMensagem>(json, JsonOptions)
                ?? throw new InvalidOperationException("JSON da fila inválido.");

            statusEmail = await _emailSender.EnviarAsync(payload.Usuario, payload.Descricao, cancellationToken);
        }
        catch (Exception ex)
        {
            statusEmail = $"Falha: {ex.Message}";
            _logger.LogError(ex, "Erro ao processar mensagem {MensagemFilaId} da fila.", mensagemFilaId);
        }

        if (payload is not null)
        {
            await _inbox.GravarAsync(
                new InboxMessage
                {
                    MensagemFilaId = mensagemFilaId,
                    DataHora = DateTime.UtcNow,
                    NomeFila = _rabbitOptions.NomeFila,
                    Usuario = payload.Usuario,
                    Descricao = payload.Descricao,
                    StatusEnvioEmail = statusEmail
                },
                cancellationToken);
        }

        _channel!.BasicAck(args.DeliveryTag, multiple: false);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
