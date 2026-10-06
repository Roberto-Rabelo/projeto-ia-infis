namespace Api.Conhecimento.Rag;

public interface IMarkdownChunker
{
    IReadOnlyList<string> Dividir(string conteudo);
}
