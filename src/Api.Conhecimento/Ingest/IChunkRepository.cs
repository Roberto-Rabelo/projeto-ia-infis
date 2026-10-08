namespace Api.Conhecimento.Ingest;

public sealed record ChunkPersistencia(
    string IdAgente,
    string CodigoDocumento,
    string NomeDocumento,
    DateOnly? DataValidade,
    string Usuario,
    string Texto,
    float[] Embedding);

public sealed record ChunkConsulta(string NomeDocumento, string Texto, float[] Embedding);

public interface IChunkRepository
{
    Task SubstituirDocumentoAsync(
        string codigoDocumento,
        IReadOnlyList<ChunkPersistencia> chunks,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChunkConsulta>> ListarVigentesAsync(CancellationToken cancellationToken);
}
