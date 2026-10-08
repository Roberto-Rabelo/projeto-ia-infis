using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

[ApiController]
[Route("api/v1/atendimento")]
public class AtendimentoController : ControllerBase
{
    private readonly IAtendimentoService _atendimentoService;

    public AtendimentoController(IAtendimentoService atendimentoService)
    {
        _atendimentoService = atendimentoService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AtendimentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AtendimentoResponse>> PostAsync(
        [FromBody] AtendimentoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario))
        {
            return BadRequest("O nome do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Pergunta))
        {
            return BadRequest("A pergunta é obrigatória.");
        }

        var resposta = await _atendimentoService.ResponderAsync(
            request.Usuario.Trim(),
            request.Pergunta.Trim(),
            cancellationToken);

        return Ok(resposta);
    }
}
