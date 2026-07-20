using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace IpLookupApi.Application;

// CQRS query: report ανά χώρα (Task 3 της εκφώνησης). CountryCodes = null/άδειο -> όλες οι χώρες.
public sealed record GetReportQuery(IReadOnlyCollection<string> CountryCodes) : IRequest<List<ReportItem>>;

public sealed class GetReportQueryHandler : IRequestHandler<GetReportQuery, List<ReportItem>>
{
    private readonly IReportRepository _reportRepository;

    public GetReportQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public Task<List<ReportItem>> Handle(GetReportQuery request, CancellationToken cancellationToken)
    {
        return _reportRepository.GetReportAsync(request.CountryCodes, cancellationToken);
    }
}
