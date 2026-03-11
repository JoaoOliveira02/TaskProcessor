using System.Diagnostics;
using MassTransit;
using TaskProcessor.Domain.Interfaces;
using TaskProcessor.Infrastructure.Messaging;

namespace TaskProcessor.Worker.Consumers;

public class JobConsumer(IJobRepository repository, ILogger<JobConsumer> logger) : IConsumer<JobCreatedMessage>
{
    private const int MaxRetries = 3;

    public async Task Consume(ConsumeContext<JobCreatedMessage> context)
    {
        var stopwatch = Stopwatch.StartNew();
        var jobId = context.Message.JobId;
        var ct = context.CancellationToken;

        logger.LogInformation("Iniciando consumo do job {JobId}", jobId);

        var job = await repository.GetByIdAsync(jobId, ct);

        if (job is null)
        {
            logger.LogWarning("Job {JobId} não encontrado", jobId);
            return;
        }

        logger.LogInformation("Iniciando processamento do job {JobId} do tipo {Type}", job.Id, job.Type);

        job.MarkAsProcessing();
        await repository.UpdateAsync(job, ct);
        logger.LogInformation("Job {JobId} marcado como {Status}", job.Id, job.Status);

        try
        {
            if (job.Type == "ErroForçado")
                throw new Exception("Erro simulado para teste!");

            // Simulação de processamento
            await Task.Delay(10000, ct);

            job.MarkAsDone();
            await repository.UpdateAsync(job, ct);

            stopwatch.Stop();
            logger.LogInformation("Job {JobId} concluído com sucesso. Status: {Status}. Tempo de processamento: {ElapsedMs}ms",
                job.Id, job.Status, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            job.IncrementRetry();

            if (job.CanRetry(MaxRetries))
            {
                job.MarkAsFailed(ex.Message);
                await repository.UpdateAsync(job, ct);

                stopwatch.Stop();
                logger.LogWarning("Job {JobId} do tipo {Type} falhou na tentativa {Retry}/{Max}. Reenfileirando... Tempo de processamento: {ElapsedMs}ms",
                    job.Id, job.Type, job.RetryCount, MaxRetries, stopwatch.ElapsedMilliseconds);

                throw;
            }

            job.MarkAsFailed(ex.Message);
            await repository.UpdateAsync(job, ct);

            stopwatch.Stop();
            logger.LogError("Job {JobId} do tipo {Type} excedeu o limite de {Max} tentativas. Status: {Status}. Tempo de processamento: {ElapsedMs}ms",
                job.Id, job.Type, MaxRetries, job.Status, stopwatch.ElapsedMilliseconds);
        }
    }
}