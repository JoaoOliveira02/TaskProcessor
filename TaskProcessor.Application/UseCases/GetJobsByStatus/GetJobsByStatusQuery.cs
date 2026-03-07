using TaskProcessor.Domain.Enums;

namespace TaskProcessor.Application.UseCases.GetJobsByStatus;

public record GetJobsByStatusQuery(JobStatus Status);
