using Api.Atendimento.Options;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Api.Atendimento.Configuration;

public sealed class OpenAiChatClientFactory : IOpenAiChatClientFactory
{
    private readonly IOpenAiChaveGestor _chaveGestor;
    private readonly OpenAiOptions _options;
    private readonly object _lock = new();
    private ChatClient? _cliente;
    private string? _chaveVinculada;

    public OpenAiChatClientFactory(
        IOpenAiChaveGestor chaveGestor,
        IOptions<OpenAiOptions> options)
    {
        _chaveGestor = chaveGestor;
        _options = options.Value;
    }

    public ChatClient ObterCliente()
    {
        var chave = _chaveGestor.ObterChaveEfetiva();

        lock (_lock)
        {
            if (_cliente is not null && _chaveVinculada == chave)
            {
                return _cliente;
            }

            _cliente = new ChatClient(_options.ChatModel, chave);
            _chaveVinculada = chave;
            return _cliente;
        }
    }

    public void Invalidar()
    {
        lock (_lock)
        {
            _cliente = null;
            _chaveVinculada = null;
        }
    }
}
