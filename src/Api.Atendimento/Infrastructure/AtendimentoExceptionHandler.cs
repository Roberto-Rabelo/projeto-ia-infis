using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Infrastructure;

public sealed class AtendimentoExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AtendimentoExceptionHandler> _logger;

    public AtendimentoExceptionHandler(
        IHostEnvironment environment,
        ILogger<AtendimentoExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Erro ao processar requisição {Metodo} {Caminho}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var status = ObterStatusCode(exception);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ObterTitulo(status, exception),
            Instance = httpContext.Request.Path
        };

        if (_environment.IsDevelopment())
        {
            problem.Detail = exception.Message;
            if (exception.InnerException is { } inner)
            {
                problem.Extensions["innerException"] = inner.Message;
                problem.Extensions["innerType"] = inner.GetType().Name;
            }

            if (exception.StackTrace is not null)
            {
                problem.Extensions["stackTrace"] = exception.StackTrace;
            }
        }
        else
        {
            problem.Detail = status switch
            {
                StatusCodes.Status502BadGateway =>
                    "Não foi possível gerar a resposta com OpenAI nem com Gemini.",
                StatusCodes.Status400BadRequest => exception.Message,
                _ => "Ocorreu um erro interno."
            };
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static int ObterStatusCode(Exception exception) =>
        exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException when EhFalhaLlm(exception) => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

    private static bool EhFalhaLlm(Exception exception)
    {
        var mensagem = exception.Message + (exception.InnerException?.Message ?? string.Empty);
        return mensagem.Contains("OpenAI", StringComparison.OrdinalIgnoreCase)
            || mensagem.Contains("Gemini", StringComparison.OrdinalIgnoreCase)
            || mensagem.Contains("Configure a chave", StringComparison.OrdinalIgnoreCase);
    }

    private static string ObterTitulo(int status, Exception exception) =>
        status switch
        {
            StatusCodes.Status502BadGateway => "Falha nos provedores de IA (OpenAI / Gemini)",
            StatusCodes.Status400BadRequest => "Requisição inválida",
            _ => "An error occurred while processing your request."
        };
}
