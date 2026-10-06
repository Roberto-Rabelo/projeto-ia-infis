using Api.Conhecimento.Options;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

public sealed class MarkdownDocumentLoader : IDocumentLoader
{
    private readonly RagOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MarkdownDocumentLoader> _logger;

    public MarkdownDocumentLoader(
        IOptions<RagOptions> options,
        IHostEnvironment environment,
        ILogger<MarkdownDocumentLoader> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(string Arquivo, string Conteudo)>> CarregarAsync(
        CancellationToken cancellationToken)
    {
        var pasta = ResolverPasta();
        if (pasta is null)
        {
            _logger.LogWarning("Pasta de documentos não encontrada.");
            return [];
        }

        var arquivos = Directory.GetFiles(pasta, "*.md", SearchOption.AllDirectories);
        var documentos = new List<(string Arquivo, string Conteudo)>(arquivos.Length);

        foreach (var arquivo in arquivos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var conteudo = await File.ReadAllTextAsync(arquivo, cancellationToken);
            documentos.Add((Path.GetFileName(arquivo), conteudo));
        }

        _logger.LogInformation("Carregados {Quantidade} arquivos .md de {Pasta}.", documentos.Count, pasta);
        return documentos;
    }

    private string? ResolverPasta()
    {
        var candidatos = new[]
        {
            Path.Combine(_environment.ContentRootPath, _options.PastaDocumentos),
            Path.Combine(AppContext.BaseDirectory, _options.PastaDocumentos)
        };

        return candidatos.FirstOrDefault(Directory.Exists);
    }
}
