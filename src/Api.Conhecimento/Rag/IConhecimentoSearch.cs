namespace Api.Conhecimento.Rag;

public interface IConhecimentoSearch
{
    Task IngerirAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunk>> BuscarAsync(string consulta, CancellationToken cancellationToken);
}
