namespace Api.Atendimento.Services;

public interface IGeminiFallbackGerador
{
    Task<LlmRespostaGerada> GerarAsync(string prompt, CancellationToken cancellationToken);
}
