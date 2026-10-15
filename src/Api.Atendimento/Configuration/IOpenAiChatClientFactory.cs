using OpenAI.Chat;

namespace Api.Atendimento.Configuration;

public interface IOpenAiChatClientFactory
{
    ChatClient ObterCliente();

    void Invalidar();
}
