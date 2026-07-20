using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

// Pipeline behavior: πρέπει να αφήνει valid requests να περάσουν στο next(), και να
// μπλοκάρει (throw, ΧΩΡΙΣ να καλέσει next()) όταν κάποιος validator αποτυγχάνει.
public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_CallsNext_WhenNoValidatorsRegistered()
    {
        var behavior = new ValidationBehavior<GetIpInfoQuery, IpInfoResponse>(Array.Empty<IValidator<GetIpInfoQuery>>());
        var expected = new IpInfoResponse("GR", "GRC", "Greece");

        var result = await behavior.Handle(new GetIpInfoQuery("8.8.8.8"), () => Task.FromResult(expected), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_CallsNext_WhenValidatorPasses()
    {
        var validatorMock = new Mock<IValidator<GetIpInfoQuery>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GetIpInfoQuery>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<GetIpInfoQuery, IpInfoResponse>(new[] { validatorMock.Object });
        var expected = new IpInfoResponse("GR", "GRC", "Greece");

        var result = await behavior.Handle(new GetIpInfoQuery("8.8.8.8"), () => Task.FromResult(expected), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_AndDoesNotCallNext_WhenValidatorFails()
    {
        var failure = new ValidationFailure("Ip", "IP address must not be empty.");
        var validatorMock = new Mock<IValidator<GetIpInfoQuery>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GetIpInfoQuery>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { failure }));

        var behavior = new ValidationBehavior<GetIpInfoQuery, IpInfoResponse>(new[] { validatorMock.Object });
        var nextCalled = false;
        RequestHandlerDelegate<IpInfoResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new IpInfoResponse("GR", "GRC", "Greece"));
        };

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new GetIpInfoQuery(""), next, CancellationToken.None));

        Assert.False(nextCalled);
    }
}
