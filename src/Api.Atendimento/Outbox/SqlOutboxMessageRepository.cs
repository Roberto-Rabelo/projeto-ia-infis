using Api.Atendimento.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Outbox;

public sealed class SqlOutboxMessageRepository : IOutboxMessageRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlOutboxMessageRepository> _logger;

    public SqlOutboxMessageRepository(
        IOptions<SqlCustosOptions> options,
        ILogger<SqlOutboxMessageRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<long> GravarAsync(OutboxMessage mensagem, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.OutboxMessage
                (DataHoraGravacao, Transmitido, DataHoraTransmissao, NomeFila, Usuario, Descricao)
            OUTPUT INSERTED.Id
            VALUES
                (@DataHoraGravacao, @Transmitido, @DataHoraTransmissao, @NomeFila, @Usuario, @Descricao)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@DataHoraGravacao", mensagem.DataHoraGravacao);
        command.Parameters.AddWithValue("@Transmitido", mensagem.Transmitido);
        command.Parameters.AddWithValue("@DataHoraTransmissao", (object?)mensagem.DataHoraTransmissao ?? DBNull.Value);
        command.Parameters.AddWithValue("@NomeFila", (object?)mensagem.NomeFila ?? DBNull.Value);
        command.Parameters.AddWithValue("@Usuario", mensagem.Usuario);
        command.Parameters.AddWithValue("@Descricao", mensagem.Descricao);

        var id = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        _logger.LogInformation("OutboxMessage {Id} gravada para o usuário {Usuario}.", id, mensagem.Usuario);
        return id;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListarPendentesAsync(int quantidade, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (@Quantidade)
                Id, DataHoraGravacao, Transmitido, DataHoraTransmissao, NomeFila, Usuario, Descricao
            FROM dbo.OutboxMessage
            WHERE Transmitido = 0
            ORDER BY Id
            """;

        var lista = new List<OutboxMessage>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Quantidade", quantidade);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(new OutboxMessage
            {
                Id = reader.GetInt64(0),
                DataHoraGravacao = reader.GetDateTime(1),
                Transmitido = reader.GetBoolean(2),
                DataHoraTransmissao = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                NomeFila = reader.IsDBNull(4) ? null : reader.GetString(4),
                Usuario = reader.GetString(5),
                Descricao = reader.GetString(6)
            });
        }

        return lista;
    }

    public async Task MarcarTransmitidoAsync(
        long id,
        string nomeFila,
        DateTime dataHoraTransmissao,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.OutboxMessage
            SET Transmitido = 1,
                DataHoraTransmissao = @DataHoraTransmissao,
                NomeFila = @NomeFila
            WHERE Id = @Id AND Transmitido = 0
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@NomeFila", nomeFila);
        command.Parameters.AddWithValue("@DataHoraTransmissao", dataHoraTransmissao);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
        {
            _logger.LogWarning("OutboxMessage {Id} não atualizada (já transmitida ou inexistente).", id);
        }
    }
}
