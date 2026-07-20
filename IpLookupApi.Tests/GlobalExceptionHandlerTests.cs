using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Το ένα σημείο mapping exception -> HTTP status code (βλ. Program.cs: UseExceptionHandler).
// Οι controllers δεν κάνουν πλέον try/catch, οπότε αυτό το mapping πρέπει να είναι σωστό.
public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock = new();

    private GlobalExceptionHandler CreateSut() => new(_loggerMock.Object);

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Response = { Body = new MemoryStream() }
    };

    [Theory]
    [InlineData(typeof(IpValidationException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(IpCountryUnknownException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(IpLookupException), StatusCodes.Status502BadGateway)]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(NotImplementedException), StatusCodes.Status500InternalServerError)]
    public async Task TryHandleAsync_MapsExceptionType_ToExpectedStatusCode(Type exceptionType, int expectedStatusCode)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom");
        var httpContext = CreateHttpContext();
        var sut = CreateSut();

        var handled = await sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_Ip2cUnavailable_Returns503_WithRetryAfterHeader()
    {
        var httpContext = CreateHttpContext();
        var sut = CreateSut();

        await sut.TryHandleAsync(httpContext, new Ip2cUnavailableException("circuit open"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.Equal("30", httpContext.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task TryHandleAsync_FluentValidationException_Returns400_WithFieldErrorsJoined()
    {
        var failures = new[]
        {
            new ValidationFailure("Ip", "IP address must not be empty.")
        };
        var httpContext = CreateHttpContext();
        var sut = CreateSut();

        var handled = await sut.TryHandleAsync(httpContext, new ValidationException(failures), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("IP address must not be empty.", body);
    }
}
