namespace Api.Conhecimento.Configuration;

public interface IOpenAiChaveGestor
{
    OpenAiChaveInfo ObterInfo(bool incluirChaveCompleta = false);

    string ObterChaveEfetiva();

    Task AtualizarAsync(
        string apiKey,
        OpenAiChavePersistencia persistirEm,
        CancellationToken cancellationToken);
}
