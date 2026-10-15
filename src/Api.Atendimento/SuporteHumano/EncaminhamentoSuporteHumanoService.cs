using Microsoft.SemanticKernel;

namespace Api.Atendimento.SuporteHumano;

public sealed class EncaminhamentoSuporteHumanoService : IEncaminhamentoSuporteHumanoService
{
    private const string PluginName = "SuporteHumano";
    private const string FunctionName = "encaminhar_atendimento_humano";

    private readonly Kernel _kernel;
    private readonly ILogger<EncaminhamentoSuporteHumanoService> _logger;

    public EncaminhamentoSuporteHumanoService(
        EncaminhamentoSuportePlugin plugin,
        ILogger<EncaminhamentoSuporteHumanoService> logger)
    {
        _logger = logger;
        _kernel = Kernel.CreateBuilder().Build();
        _kernel.Plugins.AddFromObject(plugin, PluginName);
    }

    public async Task<long?> EncaminharSeNecessarioAsync(
        string usuario,
        bool encaminharSuporteHumano,
        string descricaoEncaminhamento,
        CancellationToken cancellationToken)
    {
        if (!encaminharSuporteHumano)
        {
            return null;
        }

        var descricao = string.IsNullOrWhiteSpace(descricaoEncaminhamento)
            ? "Encaminhamento solicitado; resumo não informado pelo modelo."
            : descricaoEncaminhamento.Trim();

        var argumentos = new KernelArguments
        {
            ["usuario"] = usuario,
            ["descricao"] = descricao
        };

        var resultado = await _kernel.InvokeAsync(
            PluginName,
            FunctionName,
            argumentos,
            cancellationToken);

        var texto = resultado.GetValue<string>() ?? string.Empty;
        _logger.LogInformation("Encaminhamento via Semantic Kernel concluído. Outbox Id={Id}.", texto);

        return long.TryParse(texto, out var id) ? id : null;
    }
}
