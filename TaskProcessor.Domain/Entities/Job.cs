using TaskProcessor.Domain.Enums;

namespace TaskProcessor.Domain.Entities;

public class Job
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public JobStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public Job(string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Status = JobStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    private Job() { }

    public void MarkAsProcessing()
    {
        Status = JobStatus.InProcessing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDone()
    {
        Status = JobStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = JobStatus.Error;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanRetry(int maxRetries) => RetryCount < maxRetries;
}
