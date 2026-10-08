namespace Api.Atendimento.Custos;

public interface IConsumoTokensRepository
{
    Task GravarAsync(ConsumoTokens consumo, CancellationToken cancellationToken);
}
