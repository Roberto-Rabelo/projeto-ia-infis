using Api.Conhecimento.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Controllers;

[ApiController]
[Route("api/v1/config/openai")]
public sealed class OpenAiChaveController : ControllerBase
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOpenAiChaveGestor _chaveGestor;
    private readonly IOpenAiEmbeddingClientFactory _embeddingClientFactory;

    public OpenAiChaveController(
        IHostEnvironment hostEnvironment,
        IOpenAiChaveGestor chaveGestor,
        IOpenAiEmbeddingClientFactory embeddingClientFactory)
    {
        _hostEnvironment = hostEnvironment;
        _chaveGestor = chaveGestor;
        _embeddingClientFactory = embeddingClientFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OpenAiChaveInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<OpenAiChaveInfo> ObterStatus([FromQuery] bool completa = false)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(_chaveGestor.ObterInfo(incluirChaveCompleta: completa));
    }

    [HttpPut]
    [ProducesResponseType(typeof(OpenAiChaveInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OpenAiChaveInfo>> AtualizarAsync(
        [FromBody] AtualizarOpenAiChaveRequest request,
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
        _embeddingClientFactory.Invalidar();

        return Ok(_chaveGestor.ObterInfo(incluirChaveCompleta: true));
    }
}
