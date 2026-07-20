using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

// Task 2: το periodic refresh job. Ελέγχουμε τη βασική λογική ανά IP -
// skip όταν η χώρα δεν άλλαξε, update+invalidate cache όταν άλλαξε, και ότι
// ένα IP2C-unavailable σε ένα IP δεν ρίχνει όλο το batch/job.
public class IpInfoUpdateJobTests
{
    private readonly Mock<IIpRepository> _ipRepositoryMock = new();
    private readonly Mock<ICountryRepository> _countryRepositoryMock = new();
    private readonly Mock<IIp2cService> _ip2cServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IJobHistoryRepository> _jobHistoryRepositoryMock = new();
    private readonly Mock<ILogger<IpInfoUpdateJob>> _loggerMock = new();

    private IpInfoUpdateJob CreateSut() => new(
        _ipRepositoryMock.Object,
        _countryRepositoryMock.Object,
        _ip2cServiceMock.Object,
        _cacheServiceMock.Object,
        _jobHistoryRepositoryMock.Object,
        _loggerMock.Object);

    private static Mock<IJobExecutionContext> CreateJobContextMock()
    {
        var mock = new Mock<IJobExecutionContext>();
        mock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    // Το job κάνει πάντα AddAsync/UpdateAsync σε JobHistory - χωρίς αυτό το setup
    // ο mock γυρνάει null Task και το Execute() σκάει με NullReferenceException.
    private void SetupJobHistoryRepository()
    {
        _jobHistoryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<JobHistory>())).Returns(Task.CompletedTask);
        _jobHistoryRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<JobHistory>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Execute_SkipsUpdate_WhenCountryUnchanged()
    {
        var ip = new Ip(new IpAddress("8.8.8.8"), "US");
        _ipRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ipRepositoryMock
            .Setup(r => r.GetBatchAsync(0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ip> { ip });
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync("8.8.8.8"))
            .ReturnsAsync(Ip2cResult.Success("US", "USA", "United States"));
        SetupJobHistoryRepository();

        var sut = CreateSut();
        await sut.Execute(CreateJobContextMock().Object);

        Assert.Equal("US", ip.CountryTwoLetterCode);
        _countryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Country>()), Times.Never);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
        _ipRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_UpdatesCountryAndInvalidatesCache_WhenCountryChanged()
    {
        var ip = new Ip(new IpAddress("8.8.8.8"), "US");
        _ipRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ipRepositoryMock
            .Setup(r => r.GetBatchAsync(0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ip> { ip });
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync("8.8.8.8"))
            .ReturnsAsync(Ip2cResult.Success("GR", "GRC", "Greece"));
        _countryRepositoryMock.Setup(r => r.GetByCodeAsync("GR")).ReturnsAsync((Country)null);
        SetupJobHistoryRepository();

        var sut = CreateSut();
        await sut.Execute(CreateJobContextMock().Object);

        Assert.Equal("GR", ip.CountryTwoLetterCode);
        _countryRepositoryMock.Verify(r => r.AddAsync(It.Is<Country>(c => c.TwoLetterCode == "GR")), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ContinuesBatch_WhenIp2cUnavailableForOneIp()
    {
        var failingIp = new Ip(new IpAddress("1.1.1.1"), "US");
        var okIp = new Ip(new IpAddress("8.8.8.8"), "US");
        _ipRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _ipRepositoryMock
            .Setup(r => r.GetBatchAsync(0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ip> { failingIp, okIp });
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync("1.1.1.1"))
            .ThrowsAsync(new Ip2cUnavailableException("circuit open"));
        _ip2cServiceMock
            .Setup(s => s.GetCountryAsync("8.8.8.8"))
            .ReturnsAsync(Ip2cResult.Success("US", "USA", "United States"));

        JobHistory captured = null;
        _jobHistoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<JobHistory>()))
            .Callback<JobHistory>(jh => captured = jh)
            .Returns(Task.CompletedTask);
        _jobHistoryRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<JobHistory>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.Execute(CreateJobContextMock().Object);

        // Το batch ολοκληρώνεται κανονικά (Completed) παρόλο που ένα IP απέτυχε.
        Assert.Equal(JobStatus.Completed, captured.Status);
        Assert.Equal(2, captured.ProcessedRecords);
        _ipRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
