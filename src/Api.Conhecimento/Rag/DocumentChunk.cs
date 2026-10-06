namespace Api.Conhecimento.Rag;

public sealed record DocumentChunk(
    string Arquivo,
    int Indice,
    string Texto,
    float[] Embedding);
