using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Ingest;

[ApiController]
[Route("api/v1/ingest")]
public class IngestController : ControllerBase
{
    private readonly IIngestService _ingestService;

    public IngestController(IIngestService ingestService)
    {
        _ingestService = ingestService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2 * 1024 * 1024 + 64 * 1024)]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngestResponse>> PostAsync(
        [FromForm] IngestRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _ingestService.IngerirAsync(request, cancellationToken);
        var corpo = new IngestResponse(resultado.Mensagem, resultado.Passos);
        return StatusCode(resultado.StatusCode, corpo);
    }
}
