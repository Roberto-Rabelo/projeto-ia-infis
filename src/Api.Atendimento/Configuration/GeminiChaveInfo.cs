namespace Api.Atendimento.Configuration;

public sealed record GeminiChaveInfo(
    bool Configurada,
    string Origem,
    string Sufixo,
    string Mascara,
    bool GerenciavelPorApi,
    string? ApiKeyCompleta = null,
    string? CaminhoUserSecrets = null);

public enum GeminiChavePersistencia
{
    Local,
    UserSecrets,
    Ambos
}

public sealed class AtualizarGeminiChaveRequest
{
    public string ApiKey { get; set; } = string.Empty;

    public GeminiChavePersistencia PersistirEm { get; set; } = GeminiChavePersistencia.UserSecrets;
}
