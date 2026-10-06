using System.ComponentModel.DataAnnotations;
using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

[ApiController]
[Route("atendimento")]
public class AtendimentoController : ControllerBase
{
    private readonly IAtendimentoService _atendimentoService;

    public AtendimentoController(IAtendimentoService atendimentoService)
    {
        _atendimentoService = atendimentoService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AtendimentoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AtendimentoResponse>> PerguntarAsync(
        [FromBody] [Required] PerguntaRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Pergunta))
        {
            return BadRequest("A pergunta é obrigatória.");
        }

        var resposta = await _atendimentoService.ResponderAsync(request.Pergunta, cancellationToken);
        return Ok(resposta);
    }
}
