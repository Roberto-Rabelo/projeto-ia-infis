namespace Api.Conhecimento.Rag;

public sealed class DocumentIngestionHostedService : IHostedService
{
    private readonly IConhecimentoSearch _search;
    private readonly ILogger<DocumentIngestionHostedService> _logger;

    public DocumentIngestionHostedService(
        IConhecimentoSearch search,
        ILogger<DocumentIngestionHostedService> logger)
    {
        _search = search;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _search.IngerirAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao ingerir documentos e gerar embeddings.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
