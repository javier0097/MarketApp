using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarketApp.Api.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case FieldValidationException validation:
                logger.LogInformation("Validation failed: {Field} {Message}", validation.Field, validation.Message);
                problem = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [validation.Field] = [validation.Message],
                })
                {
                    Status = StatusCodes.Status400BadRequest,
                };
                break;

            default:
                // The full trace id is on every log line; its first 8 characters are enough for users to read out
                var incident = Activity.Current?.TraceId.ToHexString()[..8] ?? httpContext.TraceIdentifier;
                logger.LogError(exception, "Unexpected error, incident {Incident}", incident);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Ocurrió un error inesperado.",
                    Detail = $"Referencia del incidente: {incident}",
                };
                break;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        // The runtime type is needed so ValidationProblemDetails keeps its "errors"
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, "application/problem+json", cancellationToken);
        return true;
    }
}
