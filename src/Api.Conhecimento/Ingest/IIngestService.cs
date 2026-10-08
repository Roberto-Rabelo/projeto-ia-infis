namespace Api.Conhecimento.Ingest;

public interface IIngestService
{
    Task<IngestResultado> IngerirAsync(IngestRequest request, CancellationToken cancellationToken);
}
