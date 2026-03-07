using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.UseCases.GetJobsByStatus;

public class GetJobsByStatusHandler(IJobRepository repository)
{
    public async Task<IEnumerable<Job>> HandleAsync(GetJobsByStatusQuery query, CancellationToken cancellationToken = default)
    {
        return await repository.GetByStatusAsync(query.Status, cancellationToken);
    }
}
