namespace TaskProcessor.Infrastructure.Messaging;

public record JobCreatedMessage(Guid JobId, string Type, string Payload);