using MongoDB.Driver;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Enums;
using TaskProcessor.Domain.Interfaces;

namespace TaskProcessor.Infrastructure.Persistence;

public class JobRepository(MongoDbContext context) : IJobRepository
{
    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Jobs.Find(j => j.Id == id).FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<Job>> GetAllAsync(CancellationToken ct = default)
        => await context.Jobs.Find(_ => true).ToListAsync(ct);

    public async Task<IEnumerable<Job>> GetByStatusAsync(JobStatus status, CancellationToken ct = default)
        => await context.Jobs.Find(j => j.Status == status).ToListAsync(ct);

    public async Task AddAsync(Job job, CancellationToken ct = default)
        => await context.Jobs.InsertOneAsync(job, cancellationToken: ct);

    public async Task UpdateAsync(Job job, CancellationToken ct = default)
        => await context.Jobs.ReplaceOneAsync(j => j.Id == job.Id, job, cancellationToken: ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await context.Jobs.DeleteOneAsync(j => j.Id == id, ct);
}