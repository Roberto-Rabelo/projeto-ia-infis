using System.ClientModel;
using System.Text.Json;
using Api.Atendimento.Configuration;
using Api.Atendimento.Models;
using Api.Atendimento.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Api.Atendimento.Services;

public sealed class OpenAiRespostaGerador : IOpenAiRespostaGerador
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOpenAiChatClientFactory _chatClientFactory;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiRespostaGerador> _logger;

    public OpenAiRespostaGerador(
        IOpenAiChatClientFactory chatClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiRespostaGerador> logger)
    {
        _chatClientFactory = chatClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmRespostaGerada> GerarAsync(string prompt, CancellationToken cancellationToken)
    {
        ChatCompletionOptions chatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "resposta_atendimento",
                jsonSchema: BinaryData.FromString(RespostaAtendimentoJsonSchema.OpenAiSchema),
                jsonSchemaIsStrict: true)
        };

        ClientResult<ChatCompletion> resultado;
        try
        {
            resultado = await _chatClientFactory.ObterCliente().CompleteChatAsync(
                [new UserChatMessage(prompt)],
                chatOptions,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"OpenAI falhou (modelo '{_options.ChatModel}'): {ex.Message}",
                ex);
        }

        ChatCompletion completion = resultado.Value;
        var json = completion.Content[0].Text;
        var estruturada = JsonSerializer.Deserialize<RespostaEstruturadaLlm>(json, JsonOptions)
            ?? throw new InvalidOperationException("OpenAI retornou JSON inválido para a resposta estruturada.");

        _logger.LogInformation("Resposta estruturada gerada pelo modelo {Modelo}.", _options.ChatModel);

        return new LlmRespostaGerada(
            estruturada,
            _options.ChatModel,
            Fallback: false,
            completion.Usage?.InputTokenCount ?? 0,
            completion.Usage?.OutputTokenCount ?? 0);
    }
}
