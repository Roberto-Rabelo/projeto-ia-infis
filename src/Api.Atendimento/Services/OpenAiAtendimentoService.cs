using System.ClientModel;
using System.Text.Json;
using Api.Atendimento.Clients;
using Api.Atendimento.Custos;
using Api.Atendimento.Historico;
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
    private readonly IConversacaoHistoricoRepository _historico;
    private readonly IConsumoTokensRepository _custos;
    private readonly ChatClient _chatClient;
    private readonly OpenAiOptions _options;
    private readonly MongoHistoricoOptions _historicoOptions;
    private readonly SqlCustosOptions _custosOptions;
    private readonly ILogger<OpenAiAtendimentoService> _logger;

    public OpenAiAtendimentoService(
        IConhecimentoMcpClient conhecimento,
        IConversacaoHistoricoRepository historico,
        IConsumoTokensRepository custos,
        ChatClient chatClient,
        IOptions<OpenAiOptions> options,
        IOptions<MongoHistoricoOptions> historicoOptions,
        IOptions<SqlCustosOptions> custosOptions,
        ILogger<OpenAiAtendimentoService> logger)
    {
        _conhecimento = conhecimento;
        _historico = historico;
        _custos = custos;
        _chatClient = chatClient;
        _options = options.Value;
        _historicoOptions = historicoOptions.Value;
        _custosOptions = custosOptions.Value;
        _logger = logger;
    }

    public async Task<AtendimentoResponse> ResponderAsync(
        string usuario,
        string pergunta,
        CancellationToken cancellationToken)
    {
        var conversasAnteriores = await _historico.ListarUltimasDoUsuarioAsync(
            usuario,
            _historicoOptions.QuantidadeHistorico,
            cancellationToken);

        var contexto = await _conhecimento.BuscarConhecimentoAsync(pergunta, cancellationToken);
        _logger.LogInformation("Contexto RAG obtido via MCP para o usuário {Usuario}.", usuario);

        var historico = PromptAtendimento.FormatarHistorico(conversasAnteriores);
        var prompt = PromptAtendimento.Montar(usuario, contexto, historico, pergunta);

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

        var tokensEntrada = completion.Usage?.InputTokenCount ?? 0;
        var tokensSaida = completion.Usage?.OutputTokenCount ?? 0;
        var (custoEntrada, custoSaida, custoTotal) = CalculadoraCustoTokens.Calcular(
            tokensEntrada,
            tokensSaida,
            _custosOptions.PrecoEntradaPorMilhao,
            _custosOptions.PrecoSaidaPorMilhao);

        var resposta = new AtendimentoResponse(
            estruturada.Resposta,
            estruturada.Categoria,
            estruturada.Fontes,
            _options.ChatModel,
            Fallback: false,
            TokensEntrada: tokensEntrada,
            TokensSaida: tokensSaida,
            CustoEstimado: custoTotal);

        var agora = DateTime.UtcNow;
        var conversa = new ConversacaoHistorico
        {
            DataHora = agora,
            Usuario = usuario,
            Agente = _historicoOptions.Agente,
            Pergunta = pergunta,
            RespostaEstruturada = new RespostaEstruturadaPersistida
            {
                Resposta = resposta.Resposta,
                Categoria = resposta.Categoria,
                Fontes = resposta.Fontes,
                Modelo = resposta.Modelo,
                Fallback = resposta.Fallback,
                TokensEntrada = resposta.TokensEntrada,
                TokensSaida = resposta.TokensSaida,
                CustoEstimado = resposta.CustoEstimado
            },
            RespostaTexto = resposta.Resposta
        };

        await _historico.GravarAsync(conversa, cancellationToken);

        await _custos.GravarAsync(
            new ConsumoTokens
            {
                Agente = _historicoOptions.Agente,
                Modelo = resposta.Modelo,
                Usuario = usuario,
                DataHora = agora,
                QtdTokensEntrada = tokensEntrada,
                QtdTokensSaida = tokensSaida,
                CustoTokensEntrada = custoEntrada,
                CustoTokensSaida = custoSaida,
                CustoTotal = custoTotal,
                IdConversa = conversa.Id
            },
            cancellationToken);

        return resposta;
    }
}
