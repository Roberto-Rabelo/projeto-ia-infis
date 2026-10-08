using System.Text.Json.Serialization;

namespace Api.Atendimento.Models;

public sealed class AtendimentoRequest
{
    public string Usuario { get; set; } = string.Empty;

    public string Pergunta { get; set; } = string.Empty;
}

public sealed record AtendimentoResponse(
    string Resposta,
    string Categoria,
    string[] Fontes,
    string Modelo,
    bool Fallback,
    int TokensEntrada,
    int TokensSaida,
    decimal CustoEstimado);

public sealed class RespostaEstruturadaLlm
{
    [JsonPropertyName("resposta")]
    public string Resposta { get; set; } = string.Empty;

    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("fontes")]
    public string[] Fontes { get; set; } = [];
}
