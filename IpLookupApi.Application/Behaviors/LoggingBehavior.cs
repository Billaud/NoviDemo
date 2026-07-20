using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace IpLookupApi.Application;

// Pipeline behavior = decorator γύρω από ΚΑΘΕ MediatR request, χωρίς να χρειάζεται να
// γράφεται χειροκίνητα ανά handler (αυτό είναι το built-in ισοδύναμο του Autofac
// RegisterGenericDecorator - δεν χρειαζόμαστε Autofac, το MediatR το κάνει μόνο του
// μέσω AddOpenBehavior, βλ. ApplicationServiceRegistration). Νέο query/handler αύριο;
// Ήδη καλύπτεται, χωρίς καμία αλλαγή εδώ.
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{RequestName} failed after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
