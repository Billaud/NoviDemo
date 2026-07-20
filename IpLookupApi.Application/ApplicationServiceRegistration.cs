using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IpLookupApi.Application;

// Σαν το Ip2cServiceRegistration.AddIp2cService() στο Infrastructure: η καλωδίωση του
// layer ζει μαζί με το layer, όχι σκόρπια στο Program.cs.
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<GetIpInfoQuery>();

            // Pipeline behaviors: τρέχουν γύρω από ΚΑΘΕ handler, με αυτή τη σειρά
            // (Logging πρώτο = πιο "έξω", Validation δεύτερο = πιο κοντά στον handler).
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Σκανάρει το assembly και βρίσκει όλους τους AbstractValidator<T> (π.χ.
        // GetIpInfoQueryValidator) - δεν χρειάζεται χειροκίνητο AddScoped ανά validator.
        services.AddValidatorsFromAssemblyContaining<GetIpInfoQueryValidator>();

        return services;
    }
}
