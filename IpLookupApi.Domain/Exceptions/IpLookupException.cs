using System;

namespace IpLookupApi.Domain;

public class IpLookupException : Exception
{
    public IpLookupException(string message)
        : base(message)
    {
    }

    public IpLookupException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
