using System;
using System.Diagnostics;
using System.Threading.Tasks;

// Decorator pattern: τυλίγει ένα IIp2cService (τυπικά το "raw" Ip2cService) και προσθέτει
// logging/timing γύρω από την κλήση, χωρίς να αγγίζει τη λογική του HTTP call.
// Έτσι η Ip2cService μένει καθαρή από cross-cutting concerns (single responsibility).
public sealed class LoggingIp2cServiceDecorator : IIp2cService
{
    private readonly IIp2cService _inner;
    private readonly ILogger<LoggingIp2cServiceDecorator> _logger;

    public LoggingIp2cServiceDecorator(IIp2cService inner, ILogger<LoggingIp2cServiceDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Ip2cResult> GetCountryAsync(string ip)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("IP2C lookup starting for {Ip}", ip);

        try
        {
            var result = await _inner.GetCountryAsync(ip);
            _logger.LogInformation(
                "IP2C lookup for {Ip} completed in {ElapsedMs}ms with status {Status}",
                ip, stopwatch.ElapsedMilliseconds, result.Status);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP2C lookup for {Ip} failed after {ElapsedMs}ms", ip, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
