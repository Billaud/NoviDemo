using System;
using Xunit;

// Value object - το πιο θεμελιώδες invariant του domain: μόνο valid IPv4 γίνεται δεκτό,
// και η μετατροπή σε numeric value (χρησιμοποιείται ως cache key) πρέπει να είναι σωστή.
public class IpAddressTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenValueIsNullOrWhitespace(string value)
    {
        Assert.Throws<ArgumentException>(() => new IpAddress(value));
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("::1")]              // valid IPv6, όχι IPv4 - πρέπει να απορρίπτεται
    [InlineData("8.8.8")]            // regression: το .NET IPAddress.TryParse δέχεται αυτό ως 8.8.0.8 - ΔΕΝ πρέπει
    [InlineData("8.8")]              // ελλιπές, πρέπει ακριβώς 4 κομμάτια
    [InlineData("8.8.8.8.8")]        // παραπάνω από 4 κομμάτια
    [InlineData("256.1.1.1")]        // εκτός εύρους 0-255
    [InlineData("1.2.3.-4")]         // αρνητικό / μη ψηφίο
    [InlineData("1.2.3.")]           // κενό τελευταίο κομμάτι
    [InlineData("1.2.3.04")]         // leading zero - δεν επιτρέπεται (ασάφεια)
    [InlineData("999.999.999.999")]
    public void Constructor_Throws_WhenValueIsNotValidIpv4(string value)
    {
        Assert.Throws<ArgumentException>(() => new IpAddress(value));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    public void Constructor_AcceptsValidIpv4_AndExposesNormalizedValue(string value)
    {
        var ip = new IpAddress(value);

        Assert.Equal(value, ip.Address);
    }

    [Theory]
    [InlineData("0.0.0.1", 1u)]
    [InlineData("0.0.1.0", 256u)]
    public void Constructor_ComputesExpectedNumericValue(string value, uint expected)
    {
        var ip = new IpAddress(value);

        Assert.Equal(expected, ip.NumericValue);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForDifferentInstancesOfSameAddress()
    {
        var a = new IpAddress("8.8.8.8");
        var b = new IpAddress("8.8.8.8");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentAddresses()
    {
        var a = new IpAddress("8.8.8.8");
        var b = new IpAddress("1.1.1.1");

        Assert.NotEqual(a, b);
    }
}
