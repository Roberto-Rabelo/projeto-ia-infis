namespace Api.Atendimento.Options;

public sealed class MongoHistoricoOptions
{
    public const string SectionName = "MongoHistorico";

    public string ConnectionString { get; set; } =
        "mongodb://infis:infis@localhost:27018/?authSource=admin";

    public string Database { get; set; } = "bd-historico-atendimentos";

    public string Collection { get; set; } = "conversas";

    public string Agente { get; set; } = "Api.Atendimento";

    public int QuantidadeHistorico { get; set; } = 3;
}
