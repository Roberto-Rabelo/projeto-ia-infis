using Api.Atendimento.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

[ApiController]
[Route("api/v1/config/gemini")]
public sealed class GeminiChaveController : ControllerBase
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IGeminiChaveGestor _chaveGestor;

    public GeminiChaveController(
        IHostEnvironment hostEnvironment,
        IGeminiChaveGestor chaveGestor)
    {
        _hostEnvironment = hostEnvironment;
        _chaveGestor = chaveGestor;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GeminiChaveInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GeminiChaveInfo> ObterStatus([FromQuery] bool completa = false)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(_chaveGestor.ObterInfo(incluirChaveCompleta: completa));
    }

    [HttpPut]
    [ProducesResponseType(typeof(GeminiChaveInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeminiChaveInfo>> AtualizarAsync(
        [FromBody] AtualizarGeminiChaveRequest request,
        CancellationToken cancellationToken)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest("Informe apiKey no corpo JSON.");
        }

        await _chaveGestor.AtualizarAsync(request.ApiKey, request.PersistirEm, cancellationToken);

        return Ok(_chaveGestor.ObterInfo(incluirChaveCompleta: true));
    }
}
