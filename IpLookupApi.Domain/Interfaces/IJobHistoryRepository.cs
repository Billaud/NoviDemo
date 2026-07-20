using System.Threading.Tasks;

namespace IpLookupApi.Domain;

public interface IJobHistoryRepository
{
    Task AddAsync(JobHistory jobHistory);
    Task UpdateAsync(JobHistory jobHistory);
    Task<JobHistory> GetByIdAsync(long id);
}
