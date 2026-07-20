using System.Threading.Tasks;

public interface ICountryRepository
{
    Task<Country> GetByCodeAsync(string twoLetterCode);
    Task AddAsync(Country country);
}
