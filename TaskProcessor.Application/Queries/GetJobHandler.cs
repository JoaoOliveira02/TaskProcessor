using Microsoft.Extensions.Logging;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.Queries;

public record GetJobQuery(Guid Id);

public class GetJobHandler(
    IJobRepository repository,
    ILogger<GetJobHandler> logger) : IQueryHandler<GetJobQuery, Job?>
{
    public async Task<Job?> HandleAsync(GetJobQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Consultando job {JobId}", query.Id);

        var job = await repository.GetByIdAsync(query.Id, cancellationToken);

        if (job is not null)
        {
            logger.LogInformation("Job {JobId} encontrado com status {Status}", job.Id, job.Status);
        }
        else
        {
            logger.LogInformation("Job {JobId} não encontrado", query.Id);
        }

        return job;
    }
}