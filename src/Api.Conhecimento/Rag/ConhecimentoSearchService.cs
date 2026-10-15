using Api.Conhecimento.Ingest;
using Api.Conhecimento.Options;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

public sealed class ConhecimentoSearchService : IConhecimentoSearch
{
    private readonly IDocumentLoader _loader;
    private readonly IMarkdownChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;
    private readonly IChunkRepository _repository;
    private readonly RagOptions _options;
    private readonly ILogger<ConhecimentoSearchService> _logger;

    public ConhecimentoSearchService(
        IDocumentLoader loader,
        IMarkdownChunker chunker,
        IEmbeddingService embeddings,
        IVectorStore store,
        IChunkRepository repository,
        IOptions<RagOptions> options,
        ILogger<ConhecimentoSearchService> logger)
    {
        _loader = loader;
        _chunker = chunker;
        _embeddings = embeddings;
        _store = store;
        _repository = repository;
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
        if (string.IsNullOrWhiteSpace(consulta))
        {
            return [];
        }

        var persistidos = await _repository.ListarVigentesAsync(cancellationToken);
        var indexed = persistidos.Count > 0
            ? persistidos
                .Select((chunk, indice) => new DocumentChunk(chunk.NomeDocumento, indice, chunk.Texto, chunk.Embedding))
                .ToList()
            : _store.ObterTodos().ToList();

        if (indexed.Count == 0)
        {
            return [];
        }

        var siglas = ConsultaRagTermos.ExtrairSiglas(consulta);
        var consultasEmbedding = new List<string> { consulta };
        foreach (var sigla in siglas)
        {
            if (!consultasEmbedding.Contains(sigla))
            {
                consultasEmbedding.Add(sigla);
            }
        }

        var vetoresConsulta = await _embeddings.GerarLoteAsync(consultasEmbedding, cancellationToken);

        var ranqueados = indexed
            .Select(chunk =>
            {
                var similaridadeMax = vetoresConsulta
                    .Select(vetor => SimilaridadeCosseno.Calcular(vetor, chunk.Embedding))
                    .Max();

                var boost = ConsultaRagTermos.CalcularBoostLiteral(chunk, siglas, consulta);
                return (Chunk: chunk, Score: similaridadeMax + boost, Similaridade: similaridadeMax, Boost: boost);
            })
            .OrderByDescending(item => item.Score)
            .Take(_options.QuantidadeResultados)
            .ToList();

        if (ranqueados.Count > 0)
        {
            var resumo = string.Join(
                ", ",
                ranqueados.Select(item =>
                    $"{item.Chunk.Arquivo}#{item.Chunk.Indice} score={item.Score:F3} (cos={item.Similaridade:F3}+boost={item.Boost:F3})"));

            _logger.LogInformation(
                "Busca RAG: {QuantidadeConsultas} embedding(s) ({Siglas} sigla(s)), top {TopK}: {Resumo}",
                consultasEmbedding.Count,
                siglas.Count,
                ranqueados.Count,
                resumo);
        }

        return ranqueados.Select(item => item.Chunk).ToList();
    }
}
