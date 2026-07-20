using System;

namespace IpLookupApi.Application;

// Αντιστοιχεί ένα-προς-ένα στα status codes του IP2C: 0=invalid input, 1=success, 2=unknown ip.
public enum Ip2cStatus
{
    InvalidInput = 0,
    Success = 1,
    UnknownIp = 2
}

// Factory Method pattern: ο constructor είναι private, η δημιουργία γίνεται
// μόνο μέσω των static factories, που εγγυώνται ότι κάθε status έχει τα σωστά δεδομένα
// (π.χ. δεν μπορείς να φτιάξεις Success χωρίς τα codes/name).
public sealed class Ip2cResult
{
    public Ip2cStatus Status { get; }
    public string TwoLetterCode { get; }
    public string ThreeLetterCode { get; }
    public string CountryName { get; }

    private Ip2cResult(Ip2cStatus status, string twoLetterCode, string threeLetterCode, string countryName)
    {
        Status = status;
        TwoLetterCode = twoLetterCode;
        ThreeLetterCode = threeLetterCode;
        CountryName = countryName;
    }

    public static Ip2cResult Success(string twoLetterCode, string threeLetterCode, string countryName)
    {
        if (string.IsNullOrWhiteSpace(twoLetterCode))
            throw new ArgumentException("Two letter code must not be empty.", nameof(twoLetterCode));
        if (string.IsNullOrWhiteSpace(threeLetterCode))
            throw new ArgumentException("Three letter code must not be empty.", nameof(threeLetterCode));
        if (string.IsNullOrWhiteSpace(countryName))
            throw new ArgumentException("Country name must not be empty.", nameof(countryName));

        return new Ip2cResult(Ip2cStatus.Success, twoLetterCode, threeLetterCode, countryName);
    }

    public static Ip2cResult InvalidInput() => new(Ip2cStatus.InvalidInput, null, null, null);

    public static Ip2cResult UnknownIp() => new(Ip2cStatus.UnknownIp, null, null, null);
}
