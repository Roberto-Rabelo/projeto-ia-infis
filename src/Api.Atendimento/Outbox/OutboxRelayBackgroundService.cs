using Api.Atendimento.Options;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Outbox;

public sealed class OutboxRelayBackgroundService : BackgroundService
{
    private readonly IOutboxMessageRepository _repository;
    private readonly IOutboxAtendimentoPublisher _publisher;
    private readonly OutboxRelayOptions _options;
    private readonly ILogger<OutboxRelayBackgroundService> _logger;

    public OutboxRelayBackgroundService(
        IOutboxMessageRepository repository,
        IOutboxAtendimentoPublisher publisher,
        IOptions<OutboxRelayOptions> options,
        ILogger<OutboxRelayBackgroundService> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Habilitado)
        {
            _logger.LogInformation("Relay de outbox desabilitado (OutboxRelay:Habilitado=false).");
            return;
        }

        _logger.LogInformation(
            "Relay de outbox iniciado. Intervalo {Segundos}s, lote {Lote}, fila {Fila}.",
            _options.IntervaloSegundos,
            _options.LoteMaximo,
            _options.NomeFila);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervaloSegundos));

        do
        {
            try
            {
                await ProcessarLoteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no ciclo do relay de outbox.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessarLoteAsync(CancellationToken cancellationToken)
    {
        var pendentes = await _repository.ListarPendentesAsync(_options.LoteMaximo, cancellationToken);
        if (pendentes.Count == 0)
        {
            return;
        }

        foreach (var item in pendentes)
        {
            var payload = new OutboxAtendimentoMensagem
            {
                Id = item.Id,
                Usuario = item.Usuario,
                Descricao = item.Descricao
            };

            await _publisher.PublicarAsync(payload, _options.NomeFila, cancellationToken);

            await _repository.MarcarTransmitidoAsync(
                item.Id,
                _options.NomeFila,
                DateTime.UtcNow,
                cancellationToken);
        }
    }
}
