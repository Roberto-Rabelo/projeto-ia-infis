using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Atendimento.Configuration;

public static class OpenAiChaveArquivoLocal
{
    public const string NomeArquivo = "appsettings.Local.json";

    public static async Task SalvarApiKeyAsync(
        string contentRoot,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(contentRoot, NomeArquivo);
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

        var openAi = raiz["OpenAI"] as JsonObject ?? new JsonObject();
        openAi["ApiKey"] = apiKey;
        raiz["OpenAI"] = openAi;

        var json = raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public static string? LerApiKeyDoArquivo(string contentRoot)
    {
        var path = Path.Combine(contentRoot, NomeArquivo);
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        if (JsonNode.Parse(stream) is not JsonObject raiz)
        {
            return null;
        }

        return raiz["OpenAI"]?["ApiKey"]?.GetValue<string>();
    }
}
