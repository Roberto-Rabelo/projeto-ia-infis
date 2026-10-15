namespace Api.Atendimento.Services;

internal static class RespostaAtendimentoJsonSchema
{
    public const string OpenAiSchema = """
        {
          "type": "object",
          "properties": {
            "resposta": { "type": "string" },
            "categoria": { "type": "string" },
            "fontes": {
              "type": "array",
              "items": { "type": "string" }
            },
            "encaminharSuporteHumano": { "type": "boolean" },
            "descricaoEncaminhamento": { "type": "string" }
          },
          "required": ["resposta", "categoria", "fontes", "encaminharSuporteHumano", "descricaoEncaminhamento"],
          "additionalProperties": false
        }
        """;
}
