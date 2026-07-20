using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IReportRepository
{
    // twoLetterCodes: null ή άδειο = όλες οι χώρες.
    Task<List<ReportItem>> GetReportAsync(IReadOnlyCollection<string> twoLetterCodes, CancellationToken cancellationToken = default);
}
