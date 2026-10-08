namespace Api.Atendimento.Custos;

public sealed class ConsumoTokens
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Agente { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public int QtdTokensEntrada { get; set; }

    public int QtdTokensSaida { get; set; }

    public decimal CustoTokensEntrada { get; set; }

    public decimal CustoTokensSaida { get; set; }

    public decimal CustoTotal { get; set; }

    public string IdConversa { get; set; } = string.Empty;
}
