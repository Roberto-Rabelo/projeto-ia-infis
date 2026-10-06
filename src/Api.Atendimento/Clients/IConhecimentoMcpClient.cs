namespace Api.Atendimento.Clients;

public interface IConhecimentoMcpClient
{
    Task<IReadOnlyList<FerramentaMcp>> ListarFerramentasAsync(CancellationToken cancellationToken);

    Task<string> BuscarConhecimentoAsync(string consulta, CancellationToken cancellationToken);
}

public sealed record FerramentaMcp(string Nome, string? Descricao);
