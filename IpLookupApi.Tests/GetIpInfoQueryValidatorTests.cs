using Xunit;

// Το validator που τρέχει το ValidationBehavior πριν φτάσει στον handler.
public class GetIpInfoQueryValidatorTests
{
    private readonly GetIpInfoQueryValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenIpIsEmpty()
    {
        var result = _sut.Validate(new GetIpInfoQuery(""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIpInfoQuery.Ip));
    }

    [Fact]
    public void Validate_Passes_WhenIpIsNonEmpty()
    {
        var result = _sut.Validate(new GetIpInfoQuery("8.8.8.8"));

        Assert.True(result.IsValid);
    }
}
