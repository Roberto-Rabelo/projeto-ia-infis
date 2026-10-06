using System.ComponentModel;
using Api.Conhecimento.Rag;
using ModelContextProtocol.Server;

namespace Api.Conhecimento.Tools;

[McpServerToolType]
public class ConhecimentoTools
{
    private readonly IConhecimentoSearch _search;
    private readonly ILogger<ConhecimentoTools> _logger;

    public ConhecimentoTools(IConhecimentoSearch search, ILogger<ConhecimentoTools> logger)
    {
        _search = search;
        _logger = logger;
    }

    [McpServerTool(Name = "BuscarConhecimento")]
    [Description("Busca trechos de conhecimento corporativo relevantes para a consulta.")]
    public async Task<string> BuscarConhecimento(
        [Description("Consulta ou pergunta a ser pesquisada no conhecimento corporativo.")] string consulta,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tool BuscarConhecimento acionada.");

        var trechos = await _search.BuscarAsync(consulta, cancellationToken);
        if (trechos.Count == 0)
        {
            return "Nenhum conhecimento encontrado.";
        }

        return string.Join(
            "\n\n---\n\n",
            trechos.Select(trecho => trecho.Texto));
    }
}
