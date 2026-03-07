using MassTransit;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Interfaces;
using TaskProcessor.Infrastructure.Messaging;

namespace TaskProcessor.Infrastructure.Persistence;

public class MassTransitPublisher(IPublishEndpoint publishEndpoint) : IMessagePublisher
{
    public async Task PublishAsync(Job job, CancellationToken ct = default)
        => await publishEndpoint.Publish(new JobCreatedMessage(job.Id, job.Type, job.Payload), ct);
}
