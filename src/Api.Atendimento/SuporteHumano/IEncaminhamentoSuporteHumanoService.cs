namespace Api.Atendimento.SuporteHumano;

public interface IEncaminhamentoSuporteHumanoService
{
    Task<long?> EncaminharSeNecessarioAsync(
        string usuario,
        bool encaminharSuporteHumano,
        string descricaoEncaminhamento,
        CancellationToken cancellationToken);
}
