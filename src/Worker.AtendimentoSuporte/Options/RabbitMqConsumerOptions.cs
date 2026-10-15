namespace Worker.AtendimentoSuporte.Options;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Porta { get; set; } = 5672;

    public string Usuario { get; set; } = "infis";

    public string Senha { get; set; } = "infis";

    public string VirtualHost { get; set; } = "/";

    public string NomeFila { get; set; } = "atendimentos";
}
