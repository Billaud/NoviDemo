using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IIpRepository
{
    Task<Ip> GetByAddressAsync(IpAddress ipAddress);
    Task AddAsync(Ip ip);

    // Για το periodic update job (Task 2): παίρνει τα IPs σε σελίδες των `take`,
    // ταξινομημένα by Id για σταθερή σειρά μεταξύ των batches.
    Task<List<Ip>> GetBatchAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    // Οι entities από το GetBatchAsync είναι tracked - μετά από ip.UpdateCountry(...)
    // σε όσα άλλαξαν, κάνεις ένα SaveChangesAsync για όλο το batch (όχι ένα per-item round trip).
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
