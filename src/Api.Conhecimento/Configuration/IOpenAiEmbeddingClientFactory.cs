using OpenAI.Embeddings;

namespace Api.Conhecimento.Configuration;

public interface IOpenAiEmbeddingClientFactory
{
    EmbeddingClient ObterCliente();

    void Invalidar();
}
