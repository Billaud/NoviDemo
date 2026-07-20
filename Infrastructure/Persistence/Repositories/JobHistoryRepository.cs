using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class JobHistoryRepository : IJobHistoryRepository
{
    private readonly AppDbContext _dbContext;

    public JobHistoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(JobHistory jobHistory)
    {
        _dbContext.JobHistories.Add(jobHistory);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(JobHistory jobHistory)
    {
        _dbContext.JobHistories.Update(jobHistory);
        await _dbContext.SaveChangesAsync();
    }

    public Task<JobHistory> GetByIdAsync(long id)
    {
        return _dbContext.JobHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);
    }
}
