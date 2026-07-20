using FluentValidation;

namespace IpLookupApi.Application;

// Fail-fast validation στο pipeline (πριν καν φτάσει στο cache/DB/IP2C).
// Σημείωση: το IpAddress value object κάνει ΚΑΙ αυτό δικό του validation (format IPv4) -
// δεν είναι διπλή δουλειά, είναι διαφορετικό concern: αυτό εδώ είναι application-level
// input validation (φτηνό, γρήγορο fail), το IpAddress είναι domain invariant (ισχύει
// παντού, όχι μόνο μέσω MediatR).
public sealed class GetIpInfoQueryValidator : AbstractValidator<GetIpInfoQuery>
{
    public GetIpInfoQueryValidator()
    {
        RuleFor(x => x.Ip)
            .NotEmpty()
            .WithMessage("IP address must not be empty.");
    }
}
