namespace Api.Atendimento.Historico;

public interface IConversacaoHistoricoRepository
{
    Task<IReadOnlyList<ConversacaoHistorico>> ListarUltimasDoUsuarioAsync(
        string usuario,
        int quantidade,
        CancellationToken cancellationToken);

    Task GravarAsync(ConversacaoHistorico conversa, CancellationToken cancellationToken);
}
