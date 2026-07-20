using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Decorator pattern: πρέπει να περνάει το call στο inner service αναλλοίωτο
// και να αφήνει τα exceptions να ανέβουν χωρίς να τα τυλίγει.
public class LoggingIp2cServiceDecoratorTests
{
    private readonly Mock<IIp2cService> _innerMock = new();
    private readonly Mock<ILogger<LoggingIp2cServiceDecorator>> _loggerMock = new();

    private LoggingIp2cServiceDecorator CreateSut() => new(_innerMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetCountryAsync_DelegatesToInner_AndReturnsSameResult()
    {
        var expected = Ip2cResult.Success("GR", "GRC", "Greece");
        _innerMock.Setup(s => s.GetCountryAsync("8.8.8.8")).ReturnsAsync(expected);

        var sut = CreateSut();
        var result = await sut.GetCountryAsync("8.8.8.8");

        Assert.Same(expected, result);
        _innerMock.Verify(s => s.GetCountryAsync("8.8.8.8"), Times.Once);
    }

    [Fact]
    public async Task GetCountryAsync_PropagatesException_WhenInnerThrows()
    {
        _innerMock
            .Setup(s => s.GetCountryAsync(It.IsAny<string>()))
            .ThrowsAsync(new Ip2cUnavailableException("circuit open"));

        var sut = CreateSut();

        await Assert.ThrowsAsync<Ip2cUnavailableException>(() => sut.GetCountryAsync("8.8.8.8"));
    }
}
