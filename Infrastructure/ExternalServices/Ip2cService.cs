using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Timeout;

// "Raw" υλοποίηση - μιλάει με το πραγματικό IP2C API.
// Δεν κάνει logging (αυτό είναι ευθύνη του LoggingIp2cServiceDecorator, βλ. decorator pattern).
// Retry/circuit breaker/timeout μπαίνουν απ' έξω, στο HttpClient pipeline (Ip2cServiceRegistration).
// Το interface (IIp2cService) ζει στο Application layer - αυτό είναι η Infrastructure υλοποίησή του.
public sealed class Ip2cService : IIp2cService
{
    private readonly HttpClient _httpClient;

    public Ip2cService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Ip2cResult> GetCountryAsync(string ip)
    {
        try
        {
            var raw = await _httpClient.GetStringAsync($"https://ip2c.org/{ip}");
            return Parse(raw, ip);
        }
        catch (BrokenCircuitException ex)
        {
            throw new Ip2cUnavailableException($"IP2C service unavailable (circuit open) for IP {ip}", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new Ip2cUnavailableException($"IP2C timeout for IP {ip}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new Ip2cUnavailableException($"Could not contact IP2C service for IP {ip}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Ip2cUnavailableException($"IP2C timeout for IP {ip}", ex);
        }
    }

    // Contract: "status;countryCode2;countryCode3;countryName"
    // status: 0 = invalid input, 1 = success, 2 = unknown ip
    private static Ip2cResult Parse(string raw, string ip)
    {
        var parts = raw?.Split(';') ?? Array.Empty<string>();

        if (parts.Length == 0)
            throw new IpLookupException($"IP2C returned an empty response for IP {ip}.");

        return parts[0] switch
        {
            "0" => Ip2cResult.InvalidInput(),
            "2" => Ip2cResult.UnknownIp(),
            "1" when parts.Length >= 4 => Ip2cResult.Success(parts[1], parts[2], parts[3]),
            _ => throw new IpLookupException($"IP2C returned an unexpected response for IP {ip}: '{raw}'")
        };
    }
}
