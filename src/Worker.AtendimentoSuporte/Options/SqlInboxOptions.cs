namespace Worker.AtendimentoSuporte.Options;

public sealed class SqlInboxOptions
{
    public const string SectionName = "SqlInbox";

    public string ConnectionString { get; set; } =
        "Server=localhost,1434;Database=bd-custos-ia;User Id=sa;Password=Coti@2026;TrustServerCertificate=True;";
}
