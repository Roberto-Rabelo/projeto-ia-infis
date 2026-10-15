using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Api.Atendimento.Options;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class GeminiFallbackGerador : IGeminiFallbackGerador
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGeminiChaveGestor _chaveGestor;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiFallbackGerador> _logger;

    public GeminiFallbackGerador(
        IGeminiChaveGestor chaveGestor,
        IOptions<GeminiOptions> options,
        ILogger<GeminiFallbackGerador> logger)
    {
        _chaveGestor = chaveGestor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmRespostaGerada> GerarAsync(string prompt, CancellationToken cancellationToken)
    {
        var apiKey = _chaveGestor.ObterChaveEfetiva();
        var client = new Client(apiKey: apiKey);

        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseJsonSchema = JsonNode.Parse(RespostaAtendimentoJsonSchema.OpenAiSchema)
        };

        GenerateContentResponse response;
        try
        {
            response = await client.Models.GenerateContentAsync(
                _options.ChatModel,
                prompt,
                config,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Gemini falhou (modelo '{_options.ChatModel}'): {ex.Message}. " +
                "Verifique Gemini:ApiKey / GEMINI_API_KEY e o nome do modelo.",
                ex);
        }

        var candidato = response.Candidates?.FirstOrDefault()
            ?? throw new InvalidOperationException("Gemini não retornou candidatos na resposta.");

        var texto = candidato.Content?.Parts?
            .Select(part => part.Text)
            .FirstOrDefault(fragmento => !string.IsNullOrWhiteSpace(fragmento));

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new InvalidOperationException("Gemini retornou conteúdo vazio.");
        }

        var estruturada = JsonSerializer.Deserialize<RespostaEstruturadaLlm>(texto, JsonOptions)
            ?? throw new InvalidOperationException("Gemini retornou JSON inválido para a resposta estruturada.");

        _logger.LogWarning(
            "Resposta estruturada gerada pelo fallback Gemini ({Modelo}).",
            _options.ChatModel);

        var usage = response.UsageMetadata;
        var tokensEntrada = usage?.PromptTokenCount ?? 0;
        var tokensSaida = usage?.CandidatesTokenCount ?? 0;

        return new LlmRespostaGerada(
            estruturada,
            _options.ChatModel,
            Fallback: true,
            tokensEntrada,
            tokensSaida);
    }
}
