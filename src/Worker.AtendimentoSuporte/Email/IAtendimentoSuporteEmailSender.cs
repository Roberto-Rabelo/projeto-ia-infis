namespace Worker.AtendimentoSuporte.Email;

public interface IAtendimentoSuporteEmailSender
{
    Task<string> EnviarAsync(string usuario, string descricaoHtml, CancellationToken cancellationToken);
}
