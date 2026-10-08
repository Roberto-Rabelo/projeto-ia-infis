using Api.Conhecimento.Options;
using Api.Conhecimento.Rag;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Ingest;

public sealed class IngestService : IIngestService
{
    private static readonly byte[] AssinaturaPdf = "%PDF"u8.ToArray();

    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IMarkdownChunker _chunker;
    private readonly IEmbeddingService _embeddings;
    private readonly IChunkRepository _repository;
    private readonly IVectorStore _memoryStore;
    private readonly RagOptions _options;
    private readonly ILogger<IngestService> _logger;

    public IngestService(
        IPdfTextExtractor pdfExtractor,
        IMarkdownChunker chunker,
        IEmbeddingService embeddings,
        IChunkRepository repository,
        IVectorStore memoryStore,
        IOptions<RagOptions> options,
        ILogger<IngestService> logger)
    {
        _pdfExtractor = pdfExtractor;
        _chunker = chunker;
        _embeddings = embeddings;
        _repository = repository;
        _memoryStore = memoryStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IngestResultado> IngerirAsync(IngestRequest request, CancellationToken cancellationToken)
    {
        var passos = new List<string>();

        var validacao = Validar(request, passos);
        if (validacao is not null)
        {
            return validacao;
        }

        await using var stream = request.Arquivo!.OpenReadStream();
        var (texto, paginas) = _pdfExtractor.Extrair(stream);
        Registrar(passos, $"Texto extraído do PDF ({paginas} página(s)).");

        if (string.IsNullOrWhiteSpace(texto))
        {
            Registrar(passos, "Nenhum texto encontrado no PDF.");
            return new IngestResultado(StatusCodes.Status400BadRequest, "O PDF não contém texto extraível.", passos);
        }

        var trechos = _chunker.Dividir(texto);
        Registrar(passos, $"Texto separado em {trechos.Count} chunk(s).");

        if (trechos.Count == 0)
        {
            return new IngestResultado(StatusCodes.Status400BadRequest, "Não foi possível gerar chunks do documento.", passos);
        }

        var vetores = await _embeddings.GerarLoteAsync(trechos, cancellationToken);
        Registrar(passos, $"Embeddings gerados para {vetores.Count} chunk(s).");

        var nomeDocumento = request.Arquivo.FileName;
        var persistidos = trechos
            .Zip(vetores, (trecho, vetor) => new ChunkPersistencia(
                _options.IdAgente,
                request.CodigoDocumento.Trim(),
                nomeDocumento,
                request.Validade,
                request.Usuario.Trim(),
                trecho,
                vetor))
            .ToList();

        await _repository.SubstituirDocumentoAsync(request.CodigoDocumento.Trim(), persistidos, cancellationToken);
        Registrar(passos, $"{persistidos.Count} registro(s) gravado(s) no banco rag.");

        var memoria = persistidos
            .Select((chunk, indice) => new DocumentChunk(chunk.NomeDocumento, indice, chunk.Texto, chunk.Embedding))
            .ToList();
        _memoryStore.Adicionar(memoria);
        Registrar(passos, "Índice em memória atualizado.");

        return new IngestResultado(
            StatusCodes.Status201Created,
            "Conhecimento cadastrado com sucesso",
            passos);
    }

    private IngestResultado? Validar(IngestRequest request, List<string> passos)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario))
        {
            return Falha(passos, "O nome do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.CodigoDocumento))
        {
            return Falha(passos, "O código do documento é obrigatório.");
        }

        if (request.Arquivo is null || request.Arquivo.Length == 0)
        {
            return Falha(passos, "O arquivo PDF é obrigatório.");
        }

        if (request.Arquivo.Length > _options.TamanhoMaximoArquivoBytes)
        {
            return Falha(passos, "O arquivo deve ter no máximo 2MB.");
        }

        var extensao = Path.GetExtension(request.Arquivo.FileName);
        var contentType = request.Arquivo.ContentType ?? string.Empty;
        var ehPdf = extensao.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            && (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(contentType));

        if (!ehPdf || !PossuiAssinaturaPdf(request.Arquivo))
        {
            return Falha(passos, "O arquivo deve ser um PDF válido.");
        }

        var tamanhoMb = request.Arquivo.Length / 1024d / 1024d;
        Registrar(passos, $"PDF validado ({tamanhoMb:0.00} MB).");
        return null;
    }

    private static bool PossuiAssinaturaPdf(IFormFile arquivo)
    {
        using var stream = arquivo.OpenReadStream();
        Span<byte> cabecalho = stackalloc byte[4];
        var lidos = stream.Read(cabecalho);
        return lidos == 4 && cabecalho.SequenceEqual(AssinaturaPdf);
    }

    private void Registrar(List<string> passos, string passo)
    {
        passos.Add(passo);
        _logger.LogInformation("Ingest: {Passo}", passo);
    }

    private IngestResultado Falha(List<string> passos, string mensagem)
    {
        Registrar(passos, mensagem);
        return new IngestResultado(StatusCodes.Status400BadRequest, mensagem, passos);
    }
}
