using Api.Atendimento.Models;

namespace Api.Atendimento.Services;

public sealed record LlmRespostaGerada(
    RespostaEstruturadaLlm Estruturada,
    string Modelo,
    bool Fallback,
    int TokensEntrada,
    int TokensSaida);
