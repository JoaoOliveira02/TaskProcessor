using Microsoft.Extensions.Logging;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.Queries;

public record GetAllJobsQuery;

public class GetAllJobsHandler(
    IJobRepository repository,
    ILogger<GetAllJobsHandler> logger) : IQueryHandler<GetAllJobsQuery, IEnumerable<Job>>
{
    public async Task<IEnumerable<Job>> HandleAsync(GetAllJobsQuery query, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Consultando todos os jobs");

        var jobs = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation("Consulta concluída. Total de jobs: {TotalJobs}", jobs.Count());

        return jobs;
    }
}
