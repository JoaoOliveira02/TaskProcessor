using MassTransit;
using MongoDB.Driver;
using TaskProcessor.Application.UseCases.CreateJob;
using TaskProcessor.Application.UseCases.DeleteJob;
using TaskProcessor.Application.UseCases.GetAllJobs;
using TaskProcessor.Application.UseCases.GetJob;
using TaskProcessor.Application.UseCases.GetJobsByStatus;
using TaskProcessor.Application.UseCases.UpdateJob;
using TaskProcessor.Domain.Interfaces;
using TaskProcessor.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
    });
});

builder.Services.AddScoped<IMessagePublisher, MassTransitPublisher>();

// Handlers
builder.Services.AddScoped<CreateJobHandler>();
builder.Services.AddScoped<DeleteJobHandler>();
builder.Services.AddScoped<GetAllJobsHandler>();
builder.Services.AddScoped<GetJobHandler>();
builder.Services.AddScoped<GetJobsByStatusHandler>();
builder.Services.AddScoped<UpdateJobHandler>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();