using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PaperTrade.Application.Markets;

namespace PaperTrade.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, type, title) = exception switch
        {
            ExpandedMarketAccessException => (
                StatusCodes.Status403Forbidden,
                ApiProblemTypes.Validation,
                "Expanded markets require enabled access and confirmed data display rights."),
            MarketDataUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                ApiProblemTypes.MarketDataUnavailable,
                "Market data is temporarily unavailable."),
            _ => (
                StatusCodes.Status500InternalServerError,
                ApiProblemTypes.InternalServerError,
                "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {RequestMethod} {RequestPath}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed while processing {RequestMethod} {RequestPath}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Type = type,
                    Title = title,
                    Status = status
                }
            });
    }
}
