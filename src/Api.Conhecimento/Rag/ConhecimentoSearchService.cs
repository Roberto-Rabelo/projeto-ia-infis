using Api.Conhecimento.Options;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

public sealed class ConhecimentoSearchService : IConhecimentoSearch
{
    private readonly IDocumentLoader _loader;
    private readonly IMarkdownChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;
    private readonly RagOptions _options;
    private readonly ILogger<ConhecimentoSearchService> _logger;

    public ConhecimentoSearchService(
        IDocumentLoader loader,
        IMarkdownChunker chunker,
        IEmbeddingService embeddings,
        IVectorStore store,
        IOptions<RagOptions> options,
        ILogger<ConhecimentoSearchService> logger)
    {
        _loader = loader;
        _chunker = chunker;
        _embeddings = embeddings;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IngerirAsync(CancellationToken cancellationToken)
    {
        var documentos = await _loader.CarregarAsync(cancellationToken);
        var pendentes = new List<(string Arquivo, int Indice, string Texto)>();

        foreach (var (arquivo, conteudo) in documentos)
        {
            var trechos = _chunker.Dividir(conteudo);
            for (var i = 0; i < trechos.Count; i++)
            {
                pendentes.Add((arquivo, i, trechos[i]));
            }
        }

        if (pendentes.Count == 0)
        {
            _store.Substituir([]);
            _logger.LogWarning("Nenhum chunk gerado na ingestão.");
            return;
        }

        var vetores = await _embeddings.GerarLoteAsync(
            pendentes.Select(item => item.Texto).ToList(),
            cancellationToken);

        var chunks = pendentes
            .Zip(vetores, (item, vetor) => new DocumentChunk(item.Arquivo, item.Indice, item.Texto, vetor))
            .ToList();

        _store.Substituir(chunks);
        _logger.LogInformation("Índice em memória atualizado com {Quantidade} chunks.", chunks.Count);
    }

    public async Task<IReadOnlyList<DocumentChunk>> BuscarAsync(
        string consulta,
        CancellationToken cancellationToken)
    {
        var indexed = _store.ObterTodos();
        if (indexed.Count == 0 || string.IsNullOrWhiteSpace(consulta))
        {
            return [];
        }

        var consultaEmbedding = await _embeddings.GerarAsync(consulta, cancellationToken);

        return indexed
            .Select(chunk => (Chunk: chunk, Score: SimilaridadeCosseno.Calcular(consultaEmbedding, chunk.Embedding)))
            .OrderByDescending(item => item.Score)
            .Take(_options.QuantidadeResultados)
            .Select(item => item.Chunk)
            .ToList();
    }
}
