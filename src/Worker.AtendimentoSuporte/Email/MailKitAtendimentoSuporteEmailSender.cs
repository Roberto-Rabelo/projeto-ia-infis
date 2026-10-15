using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Worker.AtendimentoSuporte.Options;

namespace Worker.AtendimentoSuporte.Email;

public sealed class MailKitAtendimentoSuporteEmailSender : IAtendimentoSuporteEmailSender
{
    private readonly SmtpNotificacaoOptions _options;
    private readonly ILogger<MailKitAtendimentoSuporteEmailSender> _logger;

    public MailKitAtendimentoSuporteEmailSender(
        IOptions<SmtpNotificacaoOptions> options,
        ILogger<MailKitAtendimentoSuporteEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> EnviarAsync(string usuario, string descricaoHtml, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.Remetente));
        message.To.Add(MailboxAddress.Parse(_options.Destinatario));
        message.Subject = $"Novo atendimento solicitado por: {usuario}";

        var body = MontarCorpoHtml(usuario, descricaoHtml);
        message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Porta, _options.UsarSsl, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("E-mail de encaminhamento enviado para {Destinatario}.", _options.Destinatario);
        return "Enviado";
    }

    private static string MontarCorpoHtml(string usuario, string descricao)
    {
        var descricaoSegura = System.Net.WebUtility.HtmlEncode(descricao);
        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"/><title>Novo atendimento</title></head>
            <body style="font-family:Segoe UI,Arial,sans-serif;background:#f4f6f8;margin:0;padding:24px;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;margin:0 auto;background:#ffffff;border-radius:8px;border:1px solid #e0e4ea;">
                <tr>
                  <td style="background:#1a3a5c;color:#ffffff;padding:16px 24px;font-size:18px;font-weight:600;">
                    CorporativoAI — Suporte humano
                  </td>
                </tr>
                <tr>
                  <td style="padding:24px;color:#1f2937;font-size:15px;line-height:1.5;">
                    <p style="margin:0 0 12px;">Foi registrada uma solicitação de atendimento humano.</p>
                    <p style="margin:0 0 8px;"><strong>Usuário:</strong> {System.Net.WebUtility.HtmlEncode(usuario)}</p>
                    <p style="margin:0 0 12px;"><strong>Resumo do atendimento:</strong></p>
                    <div style="background:#f9fafb;border-left:4px solid #1a3a5c;padding:12px 16px;white-space:pre-wrap;">{descricaoSegura}</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 24px;font-size:12px;color:#6b7280;border-top:1px solid #e5e7eb;">
                    Mensagem gerada automaticamente pelo Worker.AtendimentoSuporte.
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
