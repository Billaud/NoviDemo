using System;

namespace IpLookupApi.Domain;

public enum JobStatus
{
    Running,
    Completed,
    Failed
}

// Καταγράφει εκτελέσεις background job (π.χ. bulk sync/refresh IP -> country data).
public class JobHistory
{
    public long Id { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public JobStatus Status { get; private set; }
    public int ProcessedRecords { get; private set; }
    public int UpdatedRecords { get; private set; }

    private JobHistory() { } // για EF Core

    public static JobHistory Start()
    {
        return new JobHistory
        {
            StartedAtUtc = DateTime.UtcNow,
            Status = JobStatus.Running,
            ProcessedRecords = 0,
            UpdatedRecords = 0
        };
    }

    public void Complete(int processedRecords, int updatedRecords)
    {
        ProcessedRecords = processedRecords;
        UpdatedRecords = updatedRecords;
        Status = JobStatus.Completed;
        FinishedAtUtc = DateTime.UtcNow;
    }

    public void Fail(int processedRecords, int updatedRecords)
    {
        ProcessedRecords = processedRecords;
        UpdatedRecords = updatedRecords;
        Status = JobStatus.Failed;
        FinishedAtUtc = DateTime.UtcNow;
    }
}
