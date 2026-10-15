namespace Api.Atendimento.Outbox;

public sealed class OutboxMessage
{
    public long Id { get; set; }

    public DateTime DataHoraGravacao { get; set; }

    public bool Transmitido { get; set; }

    public DateTime? DataHoraTransmissao { get; set; }

    public string? NomeFila { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;
}
