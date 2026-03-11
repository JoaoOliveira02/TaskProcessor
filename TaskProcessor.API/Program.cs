using MassTransit;
using MongoDB.Driver;
using System.Reflection;
using TaskProcessor.Application.Commands;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Application.Queries;
using TaskProcessor.Domain.Entities;
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

// Handlers CQRS
builder.Services.AddScoped<ICommandHandler<CreateJobCommand, Guid>, CreateJobHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteJobCommand>, DeleteJobHandler>();
builder.Services.AddScoped<IQueryHandler<GetJobQuery, Job?>, GetJobHandler>();
builder.Services.AddScoped<IQueryHandler<GetAllJobsQuery, IEnumerable<Job>>, GetAllJobsHandler>();
builder.Services.AddScoped<IQueryHandler<GetJobsByStatusQuery, IEnumerable<Job>>, GetJobsByStatusHandler>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "TaskProcessor API",
        Version = "v1",
        Description = "API para criacao e gerenciamento de jobs assincronos"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();