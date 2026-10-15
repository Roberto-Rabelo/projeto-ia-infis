namespace Api.Atendimento.Outbox;

public interface IOutboxMessageRepository
{
    Task<long> GravarAsync(OutboxMessage mensagem, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> ListarPendentesAsync(int quantidade, CancellationToken cancellationToken);

    Task MarcarTransmitidoAsync(long id, string nomeFila, DateTime dataHoraTransmissao, CancellationToken cancellationToken);
}
