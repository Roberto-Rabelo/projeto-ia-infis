using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Worker.AtendimentoSuporte.Options;

namespace Worker.AtendimentoSuporte.Inbox;

public sealed class SqlInboxMessageRepository : IInboxMessageRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlInboxMessageRepository> _logger;

    public SqlInboxMessageRepository(
        IOptions<SqlInboxOptions> options,
        ILogger<SqlInboxMessageRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<long> GravarAsync(InboxMessage mensagem, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.InboxMessage
                (MensagemFilaId, DataHora, NomeFila, Usuario, Descricao, StatusEnvioEmail)
            OUTPUT INSERTED.Id
            VALUES
                (@MensagemFilaId, @DataHora, @NomeFila, @Usuario, @Descricao, @StatusEnvioEmail)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@MensagemFilaId", mensagem.MensagemFilaId);
        command.Parameters.AddWithValue("@DataHora", mensagem.DataHora);
        command.Parameters.AddWithValue("@NomeFila", mensagem.NomeFila);
        command.Parameters.AddWithValue("@Usuario", mensagem.Usuario);
        command.Parameters.AddWithValue("@Descricao", mensagem.Descricao);
        command.Parameters.AddWithValue("@StatusEnvioEmail", mensagem.StatusEnvioEmail);

        var id = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        _logger.LogInformation("InboxMessage {Id} gravada (fila {MensagemFilaId}).", id, mensagem.MensagemFilaId);
        return id;
    }
}
