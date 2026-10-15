namespace Api.Atendimento.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gemini-3.5-flash-lite";
}
