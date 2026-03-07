using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.UseCases.GetAllJobs;

public class GetAllJobsHandler(IJobRepository repository)
{
    public async Task<IEnumerable<Job>> HandleAsync(GetAllJobsQuery query, CancellationToken cancellationToken = default)
    {
        return await repository.GetAllAsync(cancellationToken);
    }
}
