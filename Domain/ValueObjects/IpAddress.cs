using System;
using System.Net;

// Value object - wraps ένα IPv4 address και το κρατάει και ως numeric value
// (χρήσιμο για indexing/caching χωρίς string comparisons).
public sealed class IpAddress : IEquatable<IpAddress>
{
    public string Value { get; }
    public uint NumericValue { get; }

    public IpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address must not be empty.", nameof(value));

        if (!IPAddress.TryParse(value, out var parsed) ||
            parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new ArgumentException($"'{value}' is not a valid IPv4 address.", nameof(value));
        }

        Value = parsed.ToString();
        NumericValue = ToNumeric(parsed);
    }

    private static uint ToNumeric(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        // GetAddressBytes είναι big-endian, IPAddress.NetworkToHostOrder θέλει int
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    public bool Equals(IpAddress other) => other != null && NumericValue == other.NumericValue;
    public override bool Equals(object obj) => Equals(obj as IpAddress);
    public override int GetHashCode() => NumericValue.GetHashCode();
    public override string ToString() => Value;
}
