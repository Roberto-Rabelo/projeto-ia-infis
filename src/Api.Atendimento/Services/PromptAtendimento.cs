namespace Api.Atendimento.Services;

public static class PromptAtendimento
{
    public static string Montar(string contexto, string pergunta) =>
        $"""
        Você é um agente corporativo.

        Responda utilizando exclusivamente
        o contexto fornecido.

        Caso o contexto não contenha a informação,
        informe que não encontrou dados suficientes.

        CONTEXTO:

        {contexto}

        PERGUNTA:

        {pergunta}
        """;
}
