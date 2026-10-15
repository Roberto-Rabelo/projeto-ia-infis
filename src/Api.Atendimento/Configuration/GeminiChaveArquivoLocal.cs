using System.Text.Json;
using System.Text.Json.Nodes;

namespace Api.Atendimento.Configuration;

public static class GeminiChaveArquivoLocal
{
    public static async Task SalvarApiKeyAsync(
        string contentRoot,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(contentRoot, OpenAiChaveArquivoLocal.NomeArquivo);
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

        var gemini = raiz["Gemini"] as JsonObject ?? new JsonObject();
        gemini["ApiKey"] = apiKey;
        raiz["Gemini"] = gemini;

        var json = raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public static string? LerApiKeyDoArquivo(string contentRoot)
    {
        var path = Path.Combine(contentRoot, OpenAiChaveArquivoLocal.NomeArquivo);
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        if (JsonNode.Parse(stream) is not JsonObject raiz)
        {
            return null;
        }

        return raiz["Gemini"]?["ApiKey"]?.GetValue<string>();
    }
}
