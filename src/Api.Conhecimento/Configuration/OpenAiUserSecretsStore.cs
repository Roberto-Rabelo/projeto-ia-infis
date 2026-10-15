using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Conhecimento.Configuration;

public static class OpenAiUserSecretsStore
{
    public const string UserSecretsId = "corporativoai-api-conhecimento";

    private const string ChaveConfig = "OpenAI:ApiKey";

    public static string ObterCaminhoArquivo()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Microsoft", "UserSecrets", UserSecretsId, "secrets.json");
    }

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

        raiz[ChaveConfig] = apiKey;
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

        return raiz[ChaveConfig]?.GetValue<string>();
    }
}
