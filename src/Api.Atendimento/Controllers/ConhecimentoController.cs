using Api.Atendimento.Clients;
using Api.Atendimento.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

[ApiController]
[Route("conhecimento")]
public class ConhecimentoController : ControllerBase
{
    private readonly IConhecimentoMcpClient _conhecimentoMcpClient;

    public ConhecimentoController(IConhecimentoMcpClient conhecimentoMcpClient)
    {
        _conhecimentoMcpClient = conhecimentoMcpClient;
    }

    [HttpGet("ferramentas")]
    [ProducesResponseType(typeof(IReadOnlyList<FerramentaMcpResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FerramentaMcpResponse>>> ListarFerramentasAsync(
        CancellationToken cancellationToken)
    {
        var ferramentas = await _conhecimentoMcpClient.ListarFerramentasAsync(cancellationToken);

        var resposta = ferramentas
            .Select(tool => new FerramentaMcpResponse(tool.Nome, tool.Descricao))
            .ToList();

        return Ok(resposta);
    }

    [HttpPost("buscar")]
    [ProducesResponseType(typeof(BuscarConhecimentoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BuscarConhecimentoResponse>> BuscarAsync(
        [FromBody] BuscarConhecimentoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _conhecimentoMcpClient.BuscarConhecimentoAsync(
            request.Consulta,
            cancellationToken);

        return Ok(new BuscarConhecimentoResponse(resultado));
    }
}
