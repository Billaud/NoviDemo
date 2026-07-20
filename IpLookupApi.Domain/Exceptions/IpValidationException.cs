namespace IpLookupApi.Domain;

// Status 0 από το IP2C: το ίδιο το IP που στείλαμε θεωρήθηκε άκυρο input.
public class IpValidationException : IpLookupException
{
    public IpValidationException(string message) : base(message)
    {
    }
}
