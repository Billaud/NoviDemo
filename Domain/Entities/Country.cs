using System;

public class Country
{
    public string TwoLetterCode { get; private set; }
    public string ThreeLetterCode { get; private set; }
    public string CountryName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Country() { } // για EF Core

    public Country(string twoLetterCode, string threeLetterCode, string countryName)
    {
        if (string.IsNullOrWhiteSpace(twoLetterCode))
            throw new ArgumentException("Two letter code must not be empty.", nameof(twoLetterCode));
        if (string.IsNullOrWhiteSpace(threeLetterCode))
            throw new ArgumentException("Three letter code must not be empty.", nameof(threeLetterCode));
        if (string.IsNullOrWhiteSpace(countryName))
            throw new ArgumentException("Country name must not be empty.", nameof(countryName));

        TwoLetterCode = twoLetterCode;
        ThreeLetterCode = threeLetterCode;
        CountryName = countryName;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
