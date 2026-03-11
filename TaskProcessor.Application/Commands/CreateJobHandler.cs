using Microsoft.Extensions.Logging;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.Commands;

public record CreateJobCommand(string Type, string Payload);

public class CreateJobHandler(
    IJobRepository repository,
    IMessagePublisher publisher,
    ILogger<CreateJobHandler> logger)
    : ICommandHandler<CreateJobCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateJobCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Criando job do tipo {Type}", command.Type);

        var job = new Job(command.Type, command.Payload);
        await repository.AddAsync(job, ct);

        logger.LogInformation("Job {JobId} salvo no banco com status {Status}", job.Id, job.Status);

        await publisher.PublishAsync(job, ct);

        logger.LogInformation("Job {JobId} publicado na fila", job.Id);

        return job.Id;
    }
}
