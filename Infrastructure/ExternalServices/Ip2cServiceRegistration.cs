using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

public static class Ip2cServiceRegistration
{
    public static IServiceCollection AddIp2cService(this IServiceCollection services)
    {
        // Το "raw" Ip2cService παίρνει HttpClient με retry/circuit breaker/timeout policies.
        services.AddHttpClient<Ip2cService>(client =>
            {
                client.BaseAddress = new Uri("https://ip2c.org/");
                client.Timeout = TimeSpan.FromSeconds(3);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());

        // Decorator pattern: το interface που βλέπει ο υπόλοιπος κώδικας (IIp2cService)
        // resolve-άρεται στο LoggingIp2cServiceDecorator, που τυλίγει το raw Ip2cService.
        services.AddScoped<IIp2cService>(sp =>
            new LoggingIp2cServiceDecorator(
                sp.GetRequiredService<Ip2cService>(),
                sp.GetRequiredService<ILogger<LoggingIp2cServiceDecorator>>()));

        return services;
    }

    // Retry με exponential backoff: 200ms, 400ms, 800ms
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
    }

    // Circuit breaker: μετά από 5 συνεχόμενα fails ανοίγει για 30s
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    // Timeout ανά προσπάθεια (χωριστό από retry/circuit breaker).
    // AsyncTimeoutPolicy<T> υλοποιεί ήδη IAsyncPolicy<T> - δεν χρειάζεται τίποτα άλλο.
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2));
    }
}
