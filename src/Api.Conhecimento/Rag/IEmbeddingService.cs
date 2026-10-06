namespace Api.Conhecimento.Rag;

public interface IEmbeddingService
{
    Task<float[]> GerarAsync(string texto, CancellationToken cancellationToken);

    Task<IReadOnlyList<float[]>> GerarLoteAsync(
        IReadOnlyList<string> textos,
        CancellationToken cancellationToken);
}
