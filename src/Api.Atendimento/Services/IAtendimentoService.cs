using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public interface IAtendimentoService
{
    Task<AtendimentoResponse> ResponderAsync(string pergunta, CancellationToken cancellationToken);
}
