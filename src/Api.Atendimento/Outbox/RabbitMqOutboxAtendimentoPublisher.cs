using System.Text;
using System.Text.Json;
using Api.Atendimento.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.Atendimento.Outbox;

public sealed class RabbitMqOutboxAtendimentoPublisher : IOutboxAtendimentoPublisher, IDisposable
{
    private readonly OutboxRelayOptions _options;
    private readonly ILogger<RabbitMqOutboxAtendimentoPublisher> _logger;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqOutboxAtendimentoPublisher(
        IOptions<OutboxRelayOptions> options,
        ILogger<RabbitMqOutboxAtendimentoPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublicarAsync(OutboxAtendimentoMensagem mensagem, string nomeFila, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mensagem, JsonOptions));
        var channel = ObterCanal();

        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = mensagem.Id.ToString();

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: nomeFila,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "Outbox {Id} publicada na fila {Fila}.",
            mensagem.Id,
            nomeFila);

        return Task.CompletedTask;
    }

    private IModel ObterCanal()
    {
        lock (_sync)
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _channel?.Dispose();
            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Porta,
                UserName = _options.Usuario,
                Password = _options.Senha,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: _options.NomeFila,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            return _channel;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
