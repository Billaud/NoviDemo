using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;

namespace IpLookupApi.Application;

// Τρέχει όλους τους FluentValidation validators (IValidator<TRequest>) ΠΡΙΝ φτάσει το
// request στον handler. Invalid -> FluentValidation.ValidationException (ο
// GlobalExceptionHandler στο Api το μεταφράζει σε 400). Αν δεν υπάρχει validator για
// ένα request (π.χ. GetReportQuery), απλά προχωράει - δεν είναι όλα τα requests
// υποχρεωμένα να έχουν validator.
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure != null)
                .ToList();

            if (failures.Count > 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
