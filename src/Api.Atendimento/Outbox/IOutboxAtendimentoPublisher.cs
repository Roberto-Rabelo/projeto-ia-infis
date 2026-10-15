namespace Api.Atendimento.Outbox;

public interface IOutboxAtendimentoPublisher
{
    Task PublicarAsync(OutboxAtendimentoMensagem mensagem, string nomeFila, CancellationToken cancellationToken);
}
