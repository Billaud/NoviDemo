using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class IpRepository : IIpRepository
{
    private readonly AppDbContext _dbContext;

    public IpRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Ip> GetByAddressAsync(IpAddress ipAddress)
    {
        return _dbContext.Ips
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Address == ipAddress.Value);
    }

    public async Task AddAsync(Ip ip)
    {
        _dbContext.Ips.Add(ip);
        await _dbContext.SaveChangesAsync();
    }

    public Task<List<Ip>> GetBatchAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        // ΧΩΡΙΣ AsNoTracking: το update job θα κάνει mutate (ip.UpdateCountry(...))
        // σε αυτές τις entities και μετά SaveChangesAsync.
        return _dbContext.Ips
            .OrderBy(i => i.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Ips.CountAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
