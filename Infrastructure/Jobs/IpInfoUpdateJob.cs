using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quartz;

// Task 2 της εκφώνησης: παίρνει όλα τα IPs από τη DB σε batches των 100,
// ξαναρωτάει το IP2C, ενημερώνει όσα άλλαξαν χώρα, invalidate-άρει το cache τους,
// και καταγράφει το τρέξιμο σε JobHistory. Quartz job, τρέχει κάθε 1 ώρα (βλ. Program.cs).
[DisallowConcurrentExecution] // αν το προηγούμενο τρέξιμο δεν έχει τελειώσει, δεν ξεκινάει δεύτερο παράλληλα
public sealed class IpInfoUpdateJob : IJob
{
    private const int BatchSize = 100;

    private readonly IIpRepository _ipRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly IIp2cService _ip2cService;
    private readonly ICacheService _cacheService;
    private readonly IJobHistoryRepository _jobHistoryRepository;
    private readonly ILogger<IpInfoUpdateJob> _logger;

    public IpInfoUpdateJob(
        IIpRepository ipRepository,
        ICountryRepository countryRepository,
        IIp2cService ip2cService,
        ICacheService cacheService,
        IJobHistoryRepository jobHistoryRepository,
        ILogger<IpInfoUpdateJob> logger)
    {
        _ipRepository = ipRepository;
        _countryRepository = countryRepository;
        _ip2cService = ip2cService;
        _cacheService = cacheService;
        _jobHistoryRepository = jobHistoryRepository;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        var jobHistory = JobHistory.Start();
        await _jobHistoryRepository.AddAsync(jobHistory);

        var processed = 0;
        var updated = 0;

        try
        {
            var total = await _ipRepository.GetTotalCountAsync(cancellationToken);
            _logger.LogInformation("IP info update job started. {Total} IPs to check.", total);

            for (var skip = 0; skip < total; skip += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = await _ipRepository.GetBatchAsync(skip, BatchSize, cancellationToken);
                var cacheKeysToInvalidate = new List<string>();

                foreach (var ip in batch)
                {
                    processed++;

                    try
                    {
                        var result = await _ip2cService.GetCountryAsync(ip.Address);

                        if (result.Status != Ip2cStatus.Success)
                        {
                            _logger.LogWarning(
                                "IP2C returned {Status} for {Ip} during refresh, skipping.",
                                result.Status, ip.Address);
                            continue;
                        }

                        if (!string.Equals(ip.CountryTwoLetterCode, result.TwoLetterCode, StringComparison.OrdinalIgnoreCase))
                        {
                            var country = await _countryRepository.GetByCodeAsync(result.TwoLetterCode);
                            if (country == null)
                            {
                                country = new Country(result.TwoLetterCode, result.ThreeLetterCode, result.CountryName);
                                await _countryRepository.AddAsync(country);
                            }

                            ip.UpdateCountry(result.TwoLetterCode);
                            updated++;

                            cacheKeysToInvalidate.Add(new IpAddress(ip.Address).NumericValue.ToString());
                        }
                    }
                    catch (Ip2cUnavailableException ex)
                    {
                        // Δεν ρίχνουμε όλο το job - προσπαθούμε ξανά στο επόμενο τρέξιμο (σε 1 ώρα).
                        _logger.LogWarning(ex, "IP2C unavailable while refreshing {Ip}, will retry next run.", ip.Address);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while refreshing {Ip}", ip.Address);
                    }
                }

                // Ένα SaveChanges ανά batch (όχι per-item round trip).
                await _ipRepository.SaveChangesAsync(cancellationToken);

                foreach (var cacheKey in cacheKeysToInvalidate)
                {
                    await _cacheService.RemoveAsync(cacheKey);
                }
            }

            jobHistory.Complete(processed, updated);
            _logger.LogInformation(
                "IP info update job finished. Processed {Processed}, updated {Updated}.", processed, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP info update job failed after processing {Processed} IPs.", processed);
            jobHistory.Fail(processed, updated);
            throw;
        }
        finally
        {
            await _jobHistoryRepository.UpdateAsync(jobHistory);
        }
    }
}
