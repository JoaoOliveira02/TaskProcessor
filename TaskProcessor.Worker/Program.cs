using MassTransit;
using MongoDB.Driver;
using TaskProcessor.Domain.Interfaces;
using TaskProcessor.Infrastructure.Persistence;
using TaskProcessor.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// MongoDB
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

builder.Services.AddSingleton<MongoDbContext>(sp =>
    new MongoDbContext(
        sp.GetRequiredService<IMongoClient>(),
        builder.Configuration["MongoDB:DatabaseName"]!));

builder.Services.AddScoped<IJobRepository, JobRepository>();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<JobConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ReceiveEndpoint("job-queue", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(15)
            ));
            e.ConfigureConsumer<JobConsumer>(ctx);
            e.PrefetchCount = 3;
        });
    });
});

builder.Build().Run();