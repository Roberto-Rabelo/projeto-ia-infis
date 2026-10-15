namespace Api.Atendimento.Services;

public interface IOpenAiRespostaGerador
{
    Task<LlmRespostaGerada> GerarAsync(string prompt, CancellationToken cancellationToken);
}
