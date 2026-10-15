using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Atendimento.Configuration;

public static class GeminiUserSecretsStore
{
    private const string ChaveGeminiApiKey = "Gemini:ApiKey";
    private const string ChaveGeminiApiKeyEnv = "GEMINI_API_KEY";

    public static string ObterCaminhoArquivo() => OpenAiUserSecretsStore.ObterCaminhoArquivo();

    public static async Task SalvarApiKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        var path = ObterCaminhoArquivo();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        JsonObject raiz;
        if (File.Exists(path))
        {
            await using var stream = File.OpenRead(path);
            raiz = JsonNode.Parse(stream) as JsonObject ?? new JsonObject();
        }
        else
        {
            raiz = new JsonObject();
        }

        raiz[ChaveGeminiApiKey] = apiKey;
        raiz[ChaveGeminiApiKeyEnv] = apiKey;

        var json = raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public static string? LerApiKey()
    {
        var path = ObterCaminhoArquivo();
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        if (JsonNode.Parse(stream) is not JsonObject raiz)
        {
            return null;
        }

        return raiz[ChaveGeminiApiKey]?.GetValue<string>()
            ?? raiz[ChaveGeminiApiKeyEnv]?.GetValue<string>();
    }
}
