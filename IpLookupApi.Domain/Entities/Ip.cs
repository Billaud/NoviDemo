using System;

namespace IpLookupApi.Domain;

// Entity που αντιστοιχεί στον πίνακα "IpAddress" του schema.
// Ονομάζεται "Ip" (όχι "IpAddress") για να μη συγκρούεται με το value object IpAddress.
// Το mapping στο table name γίνεται στο AppDbContext (ToTable("IpAddress")).
public class Ip
{
    public long Id { get; private set; }
    public string Address { get; private set; }              // VARCHAR(15), UNIQUE
    public string CountryTwoLetterCode { get; private set; } // FK -> Countries.TwoLetterCode
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Ip() { } // για EF Core

    public Ip(IpAddress ipAddress, string countryTwoLetterCode)
    {
        if (ipAddress == null)
            throw new ArgumentNullException(nameof(ipAddress));
        if (string.IsNullOrWhiteSpace(countryTwoLetterCode))
            throw new ArgumentException("Country code must not be empty.", nameof(countryTwoLetterCode));

        Address = ipAddress.Address;
        CountryTwoLetterCode = countryTwoLetterCode;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void UpdateCountry(string countryTwoLetterCode)
    {
        if (string.IsNullOrWhiteSpace(countryTwoLetterCode))
            throw new ArgumentException("Country code must not be empty.", nameof(countryTwoLetterCode));

        CountryTwoLetterCode = countryTwoLetterCode;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
