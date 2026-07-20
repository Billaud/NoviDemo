using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class GetIpInfoQueryHandlerTests
{
    private readonly Mock<ILogger<GetIpInfoQueryHandler>> _loggerMock = new();
    private readonly Mock<IIpRepository> _ipRepositoryMock = new();
    private readonly Mock<ICountryRepository> _countryRepositoryMock = new();
    private readonly Mock<IIp2cService> _ip2cServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetIpInfoQueryHandler CreateSut() => new(
        _loggerMock.Object,
        _ipRepositoryMock.Object,
        _countryRepositoryMock.Object,
        _ip2cServiceMock.Object,
        _cacheServiceMock.Object);

    [Fact]
    public async Task Handle_ReturnsFromCache_WhenCacheHit_AndDoesNotHitDbOrIp2c()
    {
        // Arrange
        var cached = new IpInfoResponse("GR", "GRC", "Greece");
        _cacheServiceMock
            .Setup(c => c.GetAsync<IpInfoResponse>(It.IsAny<string>()))
            .ReturnsAsync(cached);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetIpInfoQuery("8.8.8.8"), CancellationToken.None);

        // Assert
        Assert.Same(cached, result);
        _ipRepositoryMock.Verify(r => r.GetByAddressAsync(It.IsAny<IpAddress>()), Times.Never);
        _ip2cServiceMock.Verify(s => s.GetCountryAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFromDb_WhenCacheMiss_ButIpExistsInDb()
    {
        // Arrange
        _cacheServiceMock
            .Setup(c => c.GetAsync<IpInfoResponse>(It.IsAny<string>()))
            .ReturnsAsync((IpInfoResponse)null);

        var existingIp = new Ip(new IpAddress("8.8.8.8"), "US");
        _ipRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<IpAddress>()))
            .ReturnsAsync(existingIp);

        var country = new Country("US", "USA", "United States");
        _countryRepositoryMock
            .Setup(r => r.GetByCodeAsync("US"))
            .ReturnsAsync(country);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetIpInfoQuery("8.8.8.8"), CancellationToken.None);

        // Assert
        Assert.Equal("US", result.TwoLetterCode);
        Assert.Equal("USA", result.ThreeLetterCode);
        Assert.Equal("United States", result.CountryName);
        _ip2cServiceMock.Verify(s => s.GetCountryAsync(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<IpInfoResponse>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CallsIp2c_PersistsAndCaches_WhenNotFoundAnywhere()
    {
        // Arrange
        _cacheServiceMock
            .Setup(c => c.GetAsync<IpInfoResponse>(It.IsAny<string>()))
            .ReturnsAsync((IpInfoResponse)null);
        _ipRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<IpAddress>()))
            .ReturnsAsync((Ip)null);
        _countryRepositoryMock
            .Setup(r => r.GetByCodeAsync("GR"))
            .ReturnsAsync((Country)null);
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync("2.2.2.2"))
            .ReturnsAsync(Ip2cResult.Success("GR", "GRC", "Greece"));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetIpInfoQuery("2.2.2.2"), CancellationToken.None);

        // Assert
        Assert.Equal("GR", result.TwoLetterCode);
        Assert.Equal("Greece", result.CountryName);
        _countryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Country>()), Times.Once);
        _ipRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ip>()), Times.Once);
        _cacheServiceMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<IpInfoResponse>(), It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsIpCountryUnknownException_WhenIp2cReturnsUnknownIp()
    {
        // Arrange
        _cacheServiceMock
            .Setup(c => c.GetAsync<IpInfoResponse>(It.IsAny<string>()))
            .ReturnsAsync((IpInfoResponse)null);
        _ipRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<IpAddress>()))
            .ReturnsAsync((Ip)null);
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync("192.0.2.1"))
            .ReturnsAsync(Ip2cResult.UnknownIp());

        var sut = CreateSut();

        // Act + Assert
        await Assert.ThrowsAsync<IpCountryUnknownException>(
            () => sut.Handle(new GetIpInfoQuery("192.0.2.1"), CancellationToken.None));

        _ipRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ip>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PropagatesIp2cUnavailableException_WithoutWrapping()
    {
        // Arrange
        _cacheServiceMock
            .Setup(c => c.GetAsync<IpInfoResponse>(It.IsAny<string>()))
            .ReturnsAsync((IpInfoResponse)null);
        _ipRepositoryMock
            .Setup(r => r.GetByAddressAsync(It.IsAny<IpAddress>()))
            .ReturnsAsync((Ip)null);
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync(It.IsAny<string>()))
            .ThrowsAsync(new Ip2cUnavailableException("circuit open"));

        var sut = CreateSut();

        // Act + Assert
        await Assert.ThrowsAsync<Ip2cUnavailableException>(
            () => sut.Handle(new GetIpInfoQuery("3.3.3.3"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ThrowsArgumentException_WhenIpIsEmpty()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.Handle(new GetIpInfoQuery(""), CancellationToken.None));
    }
}
