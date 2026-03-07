using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.UseCases.CreateJob;

public class CreateJobHandler(IJobRepository repository, IMessagePublisher publisher)
{
    public async Task<Guid> HandleAsync(CreateJobCommand command, CancellationToken cancellationToken = default)
    {
        var job = new Job(command.Type, command.Payload);
        await repository.AddAsync(job, cancellationToken);
        await publisher.PublishAsync(job, cancellationToken);
        return job.Id;
    }
}
