using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Application.UseCases.UpdateJob;

public class UpdateJobHandler(IJobRepository repository)
{
    public async Task HandleAsync(UpdateJobCommand command, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetByIdAsync(command.Id, cancellationToken);
        
        if (job is null)
            throw new InvalidOperationException($"Job with ID {command.Id} not found.");

        await repository.UpdateAsync(job, cancellationToken);
    }
}
