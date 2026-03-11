using Microsoft.Extensions.Logging;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Enums;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.Queries;

public record GetJobsByStatusQuery(JobStatus Status);

public class GetJobsByStatusHandler(
    IJobRepository repository,
    ILogger<GetJobsByStatusHandler> logger) : IQueryHandler<GetJobsByStatusQuery, IEnumerable<Job>>
{
    public async Task<IEnumerable<Job>> HandleAsync(GetJobsByStatusQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Consultando jobs com status {Status}", query.Status);

        var jobs = await repository.GetByStatusAsync(query.Status, cancellationToken);

        logger.LogInformation("Consulta concluída. Total de jobs com status {Status}: {TotalJobs}", query.Status, jobs.Count());

        return jobs;
    }
}
