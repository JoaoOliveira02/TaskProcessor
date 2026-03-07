using TaskProcessor.Domain.Entities;

namespace TaskProcessor.Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(Job job, CancellationToken cancellationToken = default);
}
