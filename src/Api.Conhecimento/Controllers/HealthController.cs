using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<HealthResponse>> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Health check executado em {Api}.", "Api.Conhecimento");

        return Task.FromResult<ActionResult<HealthResponse>>(
            Ok(new HealthResponse("Api.Conhecimento", "Healthy")));
    }
}

public sealed record HealthResponse(string Servico, string Status);
