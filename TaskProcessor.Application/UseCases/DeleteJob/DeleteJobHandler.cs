using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.UseCases.DeleteJob;

public class DeleteJobHandler(IJobRepository repository)
{
    public async Task HandleAsync(DeleteJobCommand command, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetByIdAsync(command.Id, cancellationToken);
        
        if (job is null)
            throw new InvalidOperationException($"Job with ID {command.Id} not found.");

        await repository.DeleteAsync(command.Id, cancellationToken);
    }
}
