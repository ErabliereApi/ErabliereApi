using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Middlewares;

/// <summary>
/// Exception handler global de l'API
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="logger"></param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Log the unhandled error
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        // 2. Format a standardized Problem Details response
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erreur serveur",
            Detail = "Une erreur inatendue est survenue sur le serveur.",
            Instance = httpContext.Request.Path
        };

        // 3. Write response to the client
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to signal that this exception has been handled
        return true;
    }
}