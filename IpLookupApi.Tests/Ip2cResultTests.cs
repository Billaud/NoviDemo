using System;
using Xunit;

// Factory Method pattern: ελέγχει ότι κάθε static factory φτιάχνει το σωστό Status
// και ότι το Success δεν επιτρέπει άδεια/κενά δεδομένα (invariant που προστατεύει ο private constructor).
public class Ip2cResultTests
{
    [Fact]
    public void Success_SetsStatusAndAllFields()
    {
        var result = Ip2cResult.Success("GR", "GRC", "Greece");

        Assert.Equal(Ip2cStatus.Success, result.Status);
        Assert.Equal("GR", result.TwoLetterCode);
        Assert.Equal("GRC", result.ThreeLetterCode);
        Assert.Equal("Greece", result.CountryName);
    }

    [Theory]
    [InlineData(null, "GRC", "Greece")]
    [InlineData("", "GRC", "Greece")]
    [InlineData("GR", null, "Greece")]
    [InlineData("GR", "GRC", "")]
    public void Success_Throws_WhenAnyFieldIsEmpty(string two, string three, string name)
    {
        Assert.Throws<ArgumentException>(() => Ip2cResult.Success(two, three, name));
    }

    [Fact]
    public void InvalidInput_HasInvalidInputStatus_AndNullFields()
    {
        var result = Ip2cResult.InvalidInput();

        Assert.Equal(Ip2cStatus.InvalidInput, result.Status);
        Assert.Null(result.TwoLetterCode);
    }

    [Fact]
    public void UnknownIp_HasUnknownIpStatus_AndNullFields()
    {
        var result = Ip2cResult.UnknownIp();

        Assert.Equal(Ip2cStatus.UnknownIp, result.Status);
        Assert.Null(result.CountryName);
    }
}
