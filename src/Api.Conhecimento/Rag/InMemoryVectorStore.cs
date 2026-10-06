namespace Api.Conhecimento.Rag;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly object _lock = new();
    private IReadOnlyList<DocumentChunk> _chunks = [];

    public void Substituir(IReadOnlyList<DocumentChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        lock (_lock)
        {
            _chunks = chunks.ToList();
        }
    }

    public IReadOnlyList<DocumentChunk> ObterTodos()
    {
        lock (_lock)
        {
            return _chunks;
        }
    }
}
