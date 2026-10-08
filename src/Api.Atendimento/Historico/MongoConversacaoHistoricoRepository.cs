using Api.Atendimento.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Api.Atendimento.Historico;

public sealed class MongoConversacaoHistoricoRepository : IConversacaoHistoricoRepository
{
    private readonly IMongoCollection<ConversacaoHistorico> _collection;
    private readonly ILogger<MongoConversacaoHistoricoRepository> _logger;

    public MongoConversacaoHistoricoRepository(
        IMongoClient client,
        IOptions<MongoHistoricoOptions> options,
        ILogger<MongoConversacaoHistoricoRepository> logger)
    {
        var configuracao = options.Value;
        var database = client.GetDatabase(configuracao.Database);
        _collection = database.GetCollection<ConversacaoHistorico>(configuracao.Collection);
        _logger = logger;
    }

    public async Task<IReadOnlyList<ConversacaoHistorico>> ListarUltimasDoUsuarioAsync(
        string usuario,
        int quantidade,
        CancellationToken cancellationToken)
    {
        var filtro = Builders<ConversacaoHistorico>.Filter.Eq(item => item.Usuario, usuario);

        var conversas = await _collection
            .Find(filtro)
            .SortByDescending(item => item.DataHora)
            .Limit(quantidade)
            .ToListAsync(cancellationToken);

        conversas.Reverse();
        _logger.LogInformation(
            "Recuperadas {Quantidade} conversas anteriores do usuário {Usuario}.",
            conversas.Count,
            usuario);

        return conversas;
    }

    public async Task GravarAsync(ConversacaoHistorico conversa, CancellationToken cancellationToken)
    {
        await _collection.InsertOneAsync(conversa, cancellationToken: cancellationToken);
        _logger.LogInformation(
            "Conversa {Id} gravada para o usuário {Usuario}.",
            conversa.Id,
            conversa.Usuario);
    }
}
