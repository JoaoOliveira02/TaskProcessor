using MassTransit;
using TaskProcessor.Domain.Interfaces;
using TaskProcessor.Infrastructure.Messaging;

namespace TaskProcessor.Worker.Consumers;

public class JobConsumer(IJobRepository repository, ILogger<JobConsumer> logger) : IConsumer<JobCreatedMessage>
{
    private const int MaxRetries = 3;

    public async Task Consume(ConsumeContext<JobCreatedMessage> context)
    {
        var jobId = context.Message.JobId;
        var ct = context.CancellationToken;

        var job = await repository.GetByIdAsync(jobId, ct);

        if (job is null)
        {
            logger.LogWarning("Job {JobId} não encontrado.", jobId);
            return;
        }

        job.MarkAsProcessing();
        await repository.UpdateAsync(job, ct);
        logger.LogInformation("Job {JobId} em processamento...", jobId);

        try
        {
            if (job.Type == "ErroForçado")
                throw new Exception("Erro simulado para teste!");

            // Simulação de processamento
            await Task.Delay(10000, ct);

            job.MarkAsDone();
            await repository.UpdateAsync(job, ct);
            logger.LogInformation("Job {JobId} concluído com sucesso.", jobId);
        }
        catch (Exception ex)
        {
            job.IncrementRetry();

            if (job.CanRetry(MaxRetries))
            {
                job.MarkAsFailed(ex.Message);
                await repository.UpdateAsync(job, ct);
                logger.LogWarning("Job {JobId} falhou. Tentativa {Retry}/{Max}. Reenfileirando...",
                    jobId, job.RetryCount, MaxRetries);

                throw; // MassTransit vai reenfileirar automaticamente
            }

            job.MarkAsFailed(ex.Message);
            await repository.UpdateAsync(job, ct);
            logger.LogError("Job {JobId} excedeu tentativas. Marcado como erro.", jobId);
        }
    }
}