using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Atendimento.Historico;

public sealed class ConversacaoHistorico
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("dataHora")]
    public DateTime DataHora { get; set; }

    [BsonElement("usuario")]
    public string Usuario { get; set; } = string.Empty;

    [BsonElement("agente")]
    public string Agente { get; set; } = string.Empty;

    [BsonElement("pergunta")]
    public string Pergunta { get; set; } = string.Empty;

    [BsonElement("respostaEstruturada")]
    public RespostaEstruturadaPersistida RespostaEstruturada { get; set; } = new();

    [BsonElement("respostaTexto")]
    public string RespostaTexto { get; set; } = string.Empty;
}

public sealed class RespostaEstruturadaPersistida
{
    public string Resposta { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public string[] Fontes { get; set; } = [];

    public string Modelo { get; set; } = string.Empty;

    public bool Fallback { get; set; }

    public int TokensEntrada { get; set; }

    public int TokensSaida { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CustoEstimado { get; set; }

    public bool EncaminharSuporteHumano { get; set; }

    public long? IdOutboxEncaminhamento { get; set; }
}
