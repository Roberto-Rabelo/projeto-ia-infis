namespace Worker.AtendimentoSuporte.Inbox;

public sealed class InboxMessage
{
    public long Id { get; set; }

    public string MensagemFilaId { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public string NomeFila { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string StatusEnvioEmail { get; set; } = string.Empty;
}
