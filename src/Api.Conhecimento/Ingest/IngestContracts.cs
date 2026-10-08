namespace Api.Conhecimento.Ingest;

public sealed class IngestRequest
{
    public string Usuario { get; set; } = string.Empty;

    public string CodigoDocumento { get; set; } = string.Empty;

    public DateOnly? Validade { get; set; }

    public IFormFile? Arquivo { get; set; }
}

public sealed record IngestResponse(string Mensagem, IReadOnlyList<string> Passos);

public sealed record IngestResultado(int StatusCode, string Mensagem, IReadOnlyList<string> Passos);
