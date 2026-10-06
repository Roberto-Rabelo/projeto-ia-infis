using System.ClientModel;
using System.Text.Json;
using Api.Atendimento.Clients;
using Api.Atendimento.Models;
using Api.Atendimento.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Api.Atendimento.Services;

public sealed class OpenAiAtendimentoService : IAtendimentoService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConhecimentoMcpClient _conhecimento;
    private readonly ChatClient _chatClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiAtendimentoService> _logger;

    public OpenAiAtendimentoService(
        IConhecimentoMcpClient conhecimento,
        ChatClient chatClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiAtendimentoService> logger)
    {
        _conhecimento = conhecimento;
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AtendimentoResponse> ResponderAsync(string pergunta, CancellationToken cancellationToken)
    {
        var contexto = await _conhecimento.BuscarConhecimentoAsync(pergunta, cancellationToken);
        var prompt = PromptAtendimento.Montar(contexto, pergunta);

        ChatCompletionOptions chatOptions = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "resposta_atendimento",
                jsonSchema: BinaryData.FromBytes("""
                    {
                      "type": "object",
                      "properties": {
                        "resposta": { "type": "string" },
                        "categoria": { "type": "string" },
                        "fontes": {
                          "type": "array",
                          "items": { "type": "string" }
                        }
                      },
                      "required": ["resposta", "categoria", "fontes"],
                      "additionalProperties": false
                    }
                    """u8.ToArray()),
                jsonSchemaIsStrict: true)
        };

        ClientResult<ChatCompletion> resultado = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            chatOptions,
            cancellationToken);

        ChatCompletion completion = resultado.Value;
        var json = completion.Content[0].Text;
        var estruturada = JsonSerializer.Deserialize<RespostaEstruturadaLlm>(json, JsonOptions)
            ?? new RespostaEstruturadaLlm();

        _logger.LogInformation("Resposta estruturada gerada pelo modelo {Modelo}.", _options.ChatModel);

        return new AtendimentoResponse(
            estruturada.Resposta,
            estruturada.Categoria,
            estruturada.Fontes,
            _options.ChatModel,
            Fallback: false,
            TokensEntrada: completion.Usage?.InputTokenCount ?? 0,
            TokensSaida: completion.Usage?.OutputTokenCount ?? 0,
            CustoEstimado: 0m);
    }
}
