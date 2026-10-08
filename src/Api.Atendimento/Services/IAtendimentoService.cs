using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IAtendimentoService
{
    Task<AtendimentoResponse> ResponderAsync(string usuario, string pergunta, CancellationToken cancellationToken);
}
