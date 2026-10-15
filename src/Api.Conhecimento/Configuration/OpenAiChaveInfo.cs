namespace Api.Conhecimento.Configuration;

public sealed record OpenAiChaveInfo(
    bool Configurada,
    string Origem,
    string Sufixo,
    string Mascara,
    bool GerenciavelPorApi,
    string? ApiKeyCompleta = null,
    string? CaminhoUserSecrets = null);

public enum OpenAiChavePersistencia
{
    Local,
    UserSecrets,
    Ambos
}

public sealed class AtualizarOpenAiChaveRequest
{
    public string ApiKey { get; set; } = string.Empty;

    public OpenAiChavePersistencia PersistirEm { get; set; } = OpenAiChavePersistencia.UserSecrets;
}
