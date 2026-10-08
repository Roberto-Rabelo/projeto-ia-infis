using Api.Atendimento.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Custos;

public sealed class SqlConsumoTokensRepository : IConsumoTokensRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlConsumoTokensRepository> _logger;

    public SqlConsumoTokensRepository(
        IOptions<SqlCustosOptions> options,
        ILogger<SqlConsumoTokensRepository> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task GravarAsync(ConsumoTokens consumo, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.ConsumoTokens
                (Id, Agente, Modelo, Usuario, DataHora, QtdTokensEntrada, QtdTokensSaida,
                 CustoTokensEntrada, CustoTokensSaida, CustoTotal, IdConversa)
            VALUES
                (@Id, @Agente, @Modelo, @Usuario, @DataHora, @QtdTokensEntrada, @QtdTokensSaida,
                 @CustoTokensEntrada, @CustoTokensSaida, @CustoTotal, @IdConversa)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", consumo.Id);
        command.Parameters.AddWithValue("@Agente", consumo.Agente);
        command.Parameters.AddWithValue("@Modelo", consumo.Modelo);
        command.Parameters.AddWithValue("@Usuario", consumo.Usuario);
        command.Parameters.AddWithValue("@DataHora", consumo.DataHora);
        command.Parameters.AddWithValue("@QtdTokensEntrada", consumo.QtdTokensEntrada);
        command.Parameters.AddWithValue("@QtdTokensSaida", consumo.QtdTokensSaida);
        command.Parameters.AddWithValue("@CustoTokensEntrada", consumo.CustoTokensEntrada);
        command.Parameters.AddWithValue("@CustoTokensSaida", consumo.CustoTokensSaida);
        command.Parameters.AddWithValue("@CustoTotal", consumo.CustoTotal);
        command.Parameters.AddWithValue("@IdConversa", consumo.IdConversa);

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation(
            "Consumo de tokens gravado. Conversa {IdConversa}, total {CustoTotal}.",
            consumo.IdConversa,
            consumo.CustoTotal);
    }
}
