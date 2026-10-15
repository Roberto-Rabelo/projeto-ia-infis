namespace Api.Atendimento.Outbox;

public sealed class OutboxAtendimentoMensagem
{
    public long Id { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;
}
