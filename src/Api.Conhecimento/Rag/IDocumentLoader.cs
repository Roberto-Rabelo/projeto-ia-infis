namespace Api.Conhecimento.Rag;

public interface IDocumentLoader
{
    Task<IReadOnlyList<(string Arquivo, string Conteudo)>> CarregarAsync(CancellationToken cancellationToken);
}
