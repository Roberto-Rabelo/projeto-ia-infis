namespace Worker.AtendimentoSuporte.Inbox;

public interface IInboxMessageRepository
{
    Task<long> GravarAsync(InboxMessage mensagem, CancellationToken cancellationToken);
}
