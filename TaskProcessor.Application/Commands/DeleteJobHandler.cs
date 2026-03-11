using Microsoft.Extensions.Logging;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.Commands;

public record DeleteJobCommand(Guid Id);

public class DeleteJobHandler(
    IJobRepository repository,
    ILogger<DeleteJobHandler> logger) : ICommandHandler<DeleteJobCommand>
{
    public async Task HandleAsync(DeleteJobCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Excluindo job {JobId}", command.Id);

        var job = await repository.GetByIdAsync(command.Id, cancellationToken);
        
        if (job is null)
            throw new InvalidOperationException($"Job with ID {command.Id} not found.");

        await repository.DeleteAsync(command.Id, cancellationToken);

        logger.LogInformation("Job {JobId} excluído com sucesso", command.Id);
    }
}
