using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Atendimento.Configuration;

public static class OpenAiUserSecretsStore
{
    public const string UserSecretsId = "07f79b64-25c9-4d53-86f8-2172de4fbfa3";

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
