
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
 
// Το GROUP BY/aggregate (Count, Max) γίνεται ΣΚΟΠΙΜΑ server-side (SQL), όχι in-memory
// στο application: το LINQ εδώ κάτω μεταφράζεται πλήρως σε ένα SQL query (EF Core το
// κάνει αυτό χωρίς client-side evaluation για join+groupby+count/max). Αν το κάναμε
// in-memory, θα τραβούσαμε ΟΛΕΣ τις γραμμές του IpAddress στην εφαρμογή πριν μετρήσουμε -
// κακή ιδέα καθώς μεγαλώνει ο πίνακας. Το covering index στο AppDbContext
// (IX_IpAddress_CountryTwoLetterCode_UpdatedAt) κάνει αυτό το query index-only.
public sealed class ReportRepository : IReportRepository
{
    private readonly AppDbContext _dbContext;
 
    public ReportRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
 
    public async Task<List<ReportItem>> GetReportAsync(
        IReadOnlyCollection<string> twoLetterCodes,
        CancellationToken cancellationToken = default)
    {
        var query =
            from ip in _dbContext.Ips.AsNoTracking()
            join country in _dbContext.Countries.AsNoTracking()
                on ip.CountryTwoLetterCode equals country.TwoLetterCode
            select new { ip.UpdatedAtUtc, country.TwoLetterCode, country.CountryName };
 
        if (twoLetterCodes != null && twoLetterCodes.Count > 0)
        {
            var codes = twoLetterCodes.Select(c => c.ToUpperInvariant()).ToList();
            query = query.Where(x => codes.Contains(x.TwoLetterCode));
        }
 
        // Το GroupBy + Count/Max εδώ ΜΕΤΑΦΡΑΖΕΤΑΙ σε SQL (GROUP BY/COUNT/MAX) - αυτό είναι
        // το κομμάτι που πρέπει να τρέξει server-side. Το EF Core όμως δεν μπορεί να
        // μεταφράσει constructor call σε record (new ReportItem(...)) μέσα στο ίδιο
        // .Select() μαζί με τα aggregates, γι' αυτό πρώτα προβάλλουμε σε anonymous type
        // και υλοποιούμε (ToListAsync) - το αποτέλεσμα εδώ είναι ήδη 1 γραμμή ανά χώρα,
        // όχι όλος ο πίνακας IpAddress.
        var aggregated = await query
            .GroupBy(x => new { x.TwoLetterCode, x.CountryName })
            .Select(g => new
            {
                g.Key.CountryName,
                AddressesCount = g.Count(),
                LastAddressUpdated = g.Max(x => x.UpdatedAtUtc)
            })
            .OrderBy(r => r.CountryName)
            .ToListAsync(cancellationToken);
 
        // Client-side μόνο η κατασκευή του record - πάνω σε ήδη aggregated, μικρό αποτέλεσμα.
        return aggregated
            .Select(r => new ReportItem(r.CountryName, r.AddressesCount, r.LastAddressUpdated))
            .ToList();
    }
}