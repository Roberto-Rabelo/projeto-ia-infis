namespace Worker.AtendimentoSuporte.Options;

public sealed class SmtpNotificacaoOptions
{
    public const string SectionName = "SmtpNotificacao";

    public string Host { get; set; } = "localhost";

    public int Porta { get; set; } = 1025;

    public bool UsarSsl { get; set; }

    public string Remetente { get; set; } = "worker-service@infis.com.br";

    public string Destinatario { get; set; } = "atendimento-suporte@infis.com.br";
}
