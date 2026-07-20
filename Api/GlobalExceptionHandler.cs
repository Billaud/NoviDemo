using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

// Ένα σημείο για όλο το mapping exception -> HTTP status code, αντί για try/catch
// σε κάθε controller. Καταχωρείται στο Program.cs με AddExceptionHandler + UseExceptionHandler.
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = Map(exception);

        _logger.LogError(exception, "Request failed: {Message}", exception.Message);

        if (exception is Ip2cUnavailableException)
        {
            // Circuit ανοιχτό / IP2C down -> λέμε στον client πότε να ξαναδοκιμάσει.
            httpContext.Response.Headers.Append("Retry-After", "30");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Message) Map(Exception exception) => exception switch
    {
        Ip2cUnavailableException => (StatusCodes.Status503ServiceUnavailable, "IP lookup service is temporarily unavailable. Please retry later."),
        IpValidationException => (StatusCodes.Status400BadRequest, exception.Message),
        IpCountryUnknownException => (StatusCodes.Status404NotFound, exception.Message),
        ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
        IpLookupException => (StatusCodes.Status502BadGateway, exception.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
