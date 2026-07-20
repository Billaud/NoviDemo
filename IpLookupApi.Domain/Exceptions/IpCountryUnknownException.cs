namespace IpLookupApi.Domain;

// Status 2 από το IP2C: valid IP, αλλά δεν ξέρει σε ποια χώρα ανήκει.
public class IpCountryUnknownException : IpLookupException
{
    public IpCountryUnknownException(string message) : base(message)
    {
    }
}
