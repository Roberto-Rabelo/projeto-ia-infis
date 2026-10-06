namespace Api.Conhecimento.Rag;

public interface IVectorStore
{
    void Substituir(IReadOnlyList<DocumentChunk> chunks);

    IReadOnlyList<DocumentChunk> ObterTodos();
}
