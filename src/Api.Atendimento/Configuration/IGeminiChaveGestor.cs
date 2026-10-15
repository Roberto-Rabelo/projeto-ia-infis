namespace Api.Atendimento.Configuration;

public interface IGeminiChaveGestor
{
    GeminiChaveInfo ObterInfo(bool incluirChaveCompleta = false);

    string ObterChaveEfetiva();

    Task AtualizarAsync(
        string apiKey,
        GeminiChavePersistencia persistirEm,
        CancellationToken cancellationToken);
}
