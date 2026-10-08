using Npgsql;
using Pgvector;

namespace Api.Conhecimento.Ingest;

public sealed class PostgresChunkRepository : IChunkRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresChunkRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task SubstituirDocumentoAsync(
        string codigoDocumento,
        IReadOnlyList<ChunkPersistencia> chunks,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var delete = new NpgsqlCommand(
            "DELETE FROM chunks WHERE codigo_documento = @codigo",
            connection,
            transaction))
        {
            delete.Parameters.AddWithValue("codigo", codigoDocumento);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insert = """
            INSERT INTO chunks
                (id_agente, codigo_documento, nome_documento, data_validade, usuario, texto, embedding)
            VALUES
                (@idAgente, @codigoDocumento, @nomeDocumento, @dataValidade, @usuario, @texto, @embedding)
            """;

        foreach (var chunk in chunks)
        {
            await using var command = new NpgsqlCommand(insert, connection, transaction);
            command.Parameters.AddWithValue("idAgente", chunk.IdAgente);
            command.Parameters.AddWithValue("codigoDocumento", chunk.CodigoDocumento);
            command.Parameters.AddWithValue("nomeDocumento", chunk.NomeDocumento);
            var validade = command.Parameters.Add("dataValidade", NpgsqlTypes.NpgsqlDbType.Date);
            validade.Value = chunk.DataValidade.HasValue ? chunk.DataValidade.Value : DBNull.Value;
            command.Parameters.AddWithValue("usuario", chunk.Usuario);
            command.Parameters.AddWithValue("texto", chunk.Texto);
            command.Parameters.AddWithValue("embedding", new Vector(chunk.Embedding));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChunkConsulta>> ListarVigentesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT nome_documento, texto, embedding
            FROM chunks
            WHERE data_validade IS NULL OR data_validade >= CURRENT_DATE
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var chunks = new List<ChunkConsulta>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var embedding = reader.GetFieldValue<Vector>(2);
            chunks.Add(new ChunkConsulta(
                reader.GetString(0),
                reader.GetString(1),
                embedding.ToArray()));
        }

        return chunks;
    }
}
