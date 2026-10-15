namespace Api.Atendimento.Options;

public sealed class OutboxRelayOptions
{
    public const string SectionName = "OutboxRelay";

    public bool Habilitado { get; set; } = true;

    public int IntervaloSegundos { get; set; } = 60;

    public int LoteMaximo { get; set; } = 5;

    public string NomeFila { get; set; } = "atendimentos";

    public string HostName { get; set; } = "localhost";

    public int Porta { get; set; } = 5672;

    public string Usuario { get; set; } = "infis";

    public string Senha { get; set; } = "infis";

    public string VirtualHost { get; set; } = "/";
}
