using Api.Conhecimento.Options;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace Api.Conhecimento.Configuration;

public sealed class OpenAiEmbeddingClientFactory : IOpenAiEmbeddingClientFactory
{
    private readonly IOpenAiChaveGestor _chaveGestor;
    private readonly OpenAiOptions _options;
    private readonly object _lock = new();
    private EmbeddingClient? _cliente;
    private string? _chaveVinculada;

    public OpenAiEmbeddingClientFactory(
        IOpenAiChaveGestor chaveGestor,
        IOptions<OpenAiOptions> options)
    {
        _chaveGestor = chaveGestor;
        _options = options.Value;
    }

    public EmbeddingClient ObterCliente()
    {
        var chave = _chaveGestor.ObterChaveEfetiva();

        lock (_lock)
        {
            if (_cliente is not null && _chaveVinculada == chave)
            {
                return _cliente;
            }

            _cliente = new EmbeddingClient(_options.EmbeddingModel, chave);
            _chaveVinculada = chave;
            return _cliente;
        }
    }

    public void Invalidar()
    {
        lock (_lock)
        {
            _cliente = null;
            _chaveVinculada = null;
        }
    }
}
