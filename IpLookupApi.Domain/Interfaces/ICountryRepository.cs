using System.Threading.Tasks;

namespace IpLookupApi.Domain;

public interface ICountryRepository
{
    Task<Country> GetByCodeAsync(string twoLetterCode);
    Task AddAsync(Country country);
}
