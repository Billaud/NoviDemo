using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IpLookupApi.Infrastructure;

public sealed class CountryRepository : ICountryRepository
{
    private readonly AppDbContext _dbContext;

    public CountryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Country> GetByCodeAsync(string twoLetterCode)
    {
        return _dbContext.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TwoLetterCode == twoLetterCode);
    }

    public async Task AddAsync(Country country)
    {
        _dbContext.Countries.Add(country);
        await _dbContext.SaveChangesAsync();
    }
}
