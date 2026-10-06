using OpenAI.Embeddings;

namespace Api.Conhecimento.Rag;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(EmbeddingClient client, ILogger<OpenAiEmbeddingService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<float[]> GerarAsync(string texto, CancellationToken cancellationToken)
    {
        var resultado = await _client.GenerateEmbeddingAsync(texto, cancellationToken: cancellationToken);
        return resultado.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GerarLoteAsync(
        IReadOnlyList<string> textos,
        CancellationToken cancellationToken)
    {
        if (textos.Count == 0)
        {
            return [];
        }

        var resultado = await _client.GenerateEmbeddingsAsync(textos, cancellationToken: cancellationToken);
        _logger.LogInformation("Gerados {Quantidade} embeddings via OpenAI.", resultado.Value.Count);

        return resultado.Value
            .Select(item => item.ToFloats().ToArray())
            .ToList();
    }
}
