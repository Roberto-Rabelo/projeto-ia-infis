namespace Api.Conhecimento.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string PastaDocumentos { get; set; } = "Documentos";

    public int TamanhoMaximoChunk { get; set; } = 500;

    public int SobreposicaoChunk { get; set; } = 80;

    public int QuantidadeResultados { get; set; } = 3;
}
