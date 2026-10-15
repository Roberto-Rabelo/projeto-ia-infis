using System.ComponentModel;
using Api.Atendimento.Outbox;
using Microsoft.SemanticKernel;

namespace Api.Atendimento.SuporteHumano;

public sealed class EncaminhamentoSuportePlugin
{
    private readonly IOutboxMessageRepository _repository;
    private readonly ILogger<EncaminhamentoSuportePlugin> _logger;

    public EncaminhamentoSuportePlugin(
        IOutboxMessageRepository repository,
        ILogger<EncaminhamentoSuportePlugin> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [KernelFunction("encaminhar_atendimento_humano")]
    [Description("Registra na outbox SQL o encaminhamento do atendimento para suporte humano.")]
    public async Task<string> EncaminharAtendimentoHumanoAsync(
        [Description("Identificador do usuário em atendimento.")] string usuario,
        [Description("Resumo objetivo do atendimento e motivo do encaminhamento.")] string descricao)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new ArgumentException("O usuário é obrigatório.", nameof(usuario));
        }

        if (string.IsNullOrWhiteSpace(descricao))
        {
            throw new ArgumentException("A descrição é obrigatória.", nameof(descricao));
        }

        var mensagem = new OutboxMessage
        {
            DataHoraGravacao = DateTime.UtcNow,
            Transmitido = false,
            DataHoraTransmissao = null,
            NomeFila = null,
            Usuario = usuario.Trim(),
            Descricao = descricao.Trim()
        };

        var id = await _repository.GravarAsync(mensagem, CancellationToken.None);
        _logger.LogInformation(
            "Encaminhamento humano registrado via Semantic Kernel. OutboxMessage {Id}.",
            id);

        return id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
