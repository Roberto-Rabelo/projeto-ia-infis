namespace Api.Atendimento.Options;

public sealed class SqlCustosOptions
{
    public const string SectionName = "SqlCustos";

    public string ConnectionString { get; set; } =
        "Server=localhost,1434;Database=bd-custos-ia;User Id=sa;Password=Coti@2026;TrustServerCertificate=True;";

    public decimal PrecoEntradaPorMilhao { get; set; } = 0.150m;

    public decimal PrecoSaidaPorMilhao { get; set; } = 0.600m;
}
