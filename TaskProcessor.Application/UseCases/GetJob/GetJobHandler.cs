using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.UseCases.GetJob;

public class GetJobHandler(IJobRepository repository)
{
    public async Task<Job?> HandleAsync(GetJobQuery query, CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(query.Id, cancellationToken);
    }
}