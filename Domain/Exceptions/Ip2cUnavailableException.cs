using System;

// Signals a transient/infrastructure failure (circuit open, timeout, network error) -
// as opposed to IpLookupException which can also mean "bad input" or "bad response".
// Callers can catch this specifically to decide on fallback/degraded behavior.
public class Ip2cUnavailableException : IpLookupException
{
    public Ip2cUnavailableException(string message)
        : base(message)
    {
    }

    public Ip2cUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
