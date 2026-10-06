using Api.Atendimento.Options;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Api.Atendimento.Clients;

public sealed class ConhecimentoMcpClient : IConhecimentoMcpClient
{
    private const string NomeToolBusca = "BuscarConhecimento";

    private readonly McpConhecimentoOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ConhecimentoMcpClient> _logger;

    public ConhecimentoMcpClient(
        IOptions<McpConhecimentoOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<ConhecimentoMcpClient> logger)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FerramentaMcp>> ListarFerramentasAsync(CancellationToken cancellationToken)
    {
        await using var client = await CriarClienteAsync(cancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Descobertas {Quantidade} tools MCP em Api.Conhecimento.", tools.Count);

        return tools
            .Select(tool => new FerramentaMcp(tool.Name, tool.Description))
            .ToList();
    }

    public async Task<string> BuscarConhecimentoAsync(string consulta, CancellationToken cancellationToken)
    {
        await using var client = await CriarClienteAsync(cancellationToken);

        var resultado = await client.CallToolAsync(
            NomeToolBusca,
            new Dictionary<string, object?> { ["consulta"] = consulta },
            cancellationToken: cancellationToken);

        var texto = resultado.Content
            .OfType<TextContentBlock>()
            .Select(bloco => bloco.Text)
            .FirstOrDefault();

        _logger.LogInformation("Tool {Tool} chamada via MCP.", NomeToolBusca);

        return texto ?? string.Empty;
    }

    private Task<McpClient> CriarClienteAsync(CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = "Api.Conhecimento",
            Endpoint = new Uri(_options.Endpoint),
            TransportMode = HttpTransportMode.StreamableHttp
        });

        return McpClient.CreateAsync(
            transport,
            loggerFactory: _loggerFactory,
            cancellationToken: cancellationToken);
    }
}
