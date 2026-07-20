using System;

namespace IpLookupApi.Domain;

// Value object - wraps ένα IPv4 address και το κρατάει και ως numeric value
// (χρήσιμο για indexing/caching χωρίς string comparisons).
//
// ΣΚΟΠΙΜΑ δεν χρησιμοποιούμε IPAddress.TryParse: το .NET δέχεται "shortened" notations
// (π.χ. "8.8.8" -> 8.8.0.8, "8.8" -> 8.0.0.8, ή ακόμα και δεκαδικό/octal per-part), άρα
// ένα ελλιπές ή "κρυφό" IP περνάει ως valid. Το validation εδώ είναι explicit:
// ακριβώς 4 κομμάτια χωρισμένα με '.', το καθένα μόνο ψηφία, χωρίς leading zero
// (εκτός από το ίδιο το "0"), και τιμή 0-255.
public sealed class IpAddress : IEquatable<IpAddress>
{
    // Μόνο αυτά τα 2 - Address (normalized string) και NumericValue (uint, για
    // cache keys/indexing χωρίς string comparisons). Όλη η validation + η μετατροπή
    // σε numeric γίνονται ΕΔΩ, στον constructor - πουθενά αλλού στο codebase.
    public string Address { get; }
    public uint NumericValue { get; }

    public IpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address must not be empty.", nameof(value));

        var octets = ParseOctets(value);

        Address = string.Join('.', octets);
        NumericValue = ((uint)octets[0] << 24) | ((uint)octets[1] << 16) | ((uint)octets[2] << 8) | octets[3];
    }

    private static byte[] ParseOctets(string value)
    {
        var parts = value.Split('.');
        if (parts.Length != 4)
            throw new ArgumentException($"'{value}' is not a valid IPv4 address: expected 4 parts.", nameof(value));

        var octets = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            octets[i] = ParseOctet(parts[i], value);
        }

        return octets;
    }

    private static byte ParseOctet(string part, string originalValue)
    {
        if (part.Length == 0 || part.Length > 3)
            throw new ArgumentException($"'{originalValue}' is not a valid IPv4 address.", nameof(originalValue));

        foreach (var c in part)
        {
            if (!char.IsAsciiDigit(c))
                throw new ArgumentException($"'{originalValue}' is not a valid IPv4 address.", nameof(originalValue));
        }

        // "01", "007" κλπ δεν επιτρέπονται - μόνο "0" είναι valid μονοψήφιο μηδέν.
        // (αποφεύγει ασάφεια/ octal-style παρσαρίσματα που χρησιμοποιούνται σε SSRF bypasses).
        if (part.Length > 1 && part[0] == '0')
            throw new ArgumentException($"'{originalValue}' is not a valid IPv4 address: leading zeros are not allowed.", nameof(originalValue));

        var numeric = int.Parse(part);
        if (numeric is < 0 or > 255)
            throw new ArgumentException($"'{originalValue}' is not a valid IPv4 address: each part must be 0-255.", nameof(originalValue));

        return (byte)numeric;
    }

    public bool Equals(IpAddress other) => other != null && NumericValue == other.NumericValue;
    public override bool Equals(object obj) => Equals(obj as IpAddress);
    public override int GetHashCode() => NumericValue.GetHashCode();
    public override string ToString() => Address;
}
