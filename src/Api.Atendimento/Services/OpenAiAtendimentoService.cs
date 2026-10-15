using Api.Atendimento.Clients;
using Api.Atendimento.Custos;
using Api.Atendimento.Historico;
using Api.Atendimento.Models;
using Api.Atendimento.Options;
using Api.Atendimento.SuporteHumano;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Services;

public sealed class OpenAiAtendimentoService : IAtendimentoService
{
    private readonly IConhecimentoMcpClient _conhecimento;
    private readonly IConversacaoHistoricoRepository _historico;
    private readonly IConsumoTokensRepository _custos;
    private readonly IOpenAiRespostaGerador _openAi;
    private readonly IGeminiFallbackGerador _gemini;
    private readonly IEncaminhamentoSuporteHumanoService _encaminhamentoSuporte;
    private readonly MongoHistoricoOptions _historicoOptions;
    private readonly SqlCustosOptions _custosOptions;
    private readonly ILogger<OpenAiAtendimentoService> _logger;

    public OpenAiAtendimentoService(
        IConhecimentoMcpClient conhecimento,
        IConversacaoHistoricoRepository historico,
        IConsumoTokensRepository custos,
        IOpenAiRespostaGerador openAi,
        IGeminiFallbackGerador gemini,
        IEncaminhamentoSuporteHumanoService encaminhamentoSuporte,
        IOptions<MongoHistoricoOptions> historicoOptions,
        IOptions<SqlCustosOptions> custosOptions,
        ILogger<OpenAiAtendimentoService> logger)
    {
        _conhecimento = conhecimento;
        _historico = historico;
        _custos = custos;
        _openAi = openAi;
        _gemini = gemini;
        _encaminhamentoSuporte = encaminhamentoSuporte;
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

        var llm = await GerarRespostaLlmAsync(prompt, cancellationToken);

        var idOutbox = await _encaminhamentoSuporte.EncaminharSeNecessarioAsync(
            usuario,
            llm.Estruturada.EncaminharSuporteHumano,
            llm.Estruturada.DescricaoEncaminhamento,
            cancellationToken);

        var (custoEntrada, custoSaida, custoTotal) = CalculadoraCustoTokens.Calcular(
            llm.TokensEntrada,
            llm.TokensSaida,
            _custosOptions.PrecoEntradaPorMilhao,
            _custosOptions.PrecoSaidaPorMilhao);

        var resposta = new AtendimentoResponse(
            llm.Estruturada.Resposta,
            llm.Estruturada.Categoria,
            llm.Estruturada.Fontes,
            llm.Modelo,
            llm.Fallback,
            llm.TokensEntrada,
            llm.TokensSaida,
            custoTotal,
            llm.Estruturada.EncaminharSuporteHumano,
            idOutbox);

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
                CustoEstimado = resposta.CustoEstimado,
                EncaminharSuporteHumano = resposta.EncaminharSuporteHumano,
                IdOutboxEncaminhamento = resposta.IdOutboxEncaminhamento
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
                QtdTokensEntrada = llm.TokensEntrada,
                QtdTokensSaida = llm.TokensSaida,
                CustoTokensEntrada = custoEntrada,
                CustoTokensSaida = custoSaida,
                CustoTotal = custoTotal,
                IdConversa = conversa.Id
            },
            cancellationToken);

        return resposta;
    }

    private async Task<LlmRespostaGerada> GerarRespostaLlmAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _openAi.GerarAsync(prompt, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Falha na geração com OpenAI; acionando fallback Gemini.");

            try
            {
                return await _gemini.GerarAsync(prompt, cancellationToken);
            }
            catch (Exception fallbackEx) when (fallbackEx is not OperationCanceledException)
            {
                _logger.LogError(
                    fallbackEx,
                    "Fallback Gemini também falhou após erro na OpenAI.");

                throw new InvalidOperationException(
                    "Não foi possível gerar a resposta com OpenAI nem com Gemini.",
                    fallbackEx);
            }
        }
    }
}
