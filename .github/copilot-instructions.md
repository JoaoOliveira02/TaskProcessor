# Copilot Instructions — TaskProcessor

## Visão Geral do Projeto
Serviço de processamento assíncrono de tarefas (jobs) em C# / ASP.NET Core 8.
Utiliza MongoDB como banco de dados, RabbitMQ como fila de mensagens via MassTransit, e segue Clean Architecture com DDD.

---

## Arquitetura

O projeto segue **Clean Architecture**. As dependências sempre apontam para dentro:

```
API / Worker → Application → Domain
Infrastructure → Application → Domain
```

### Camadas e suas responsabilidades

- **Domain** (`TaskProcessor.Domain`): entidades, enums, interfaces de repositório. Sem dependências externas.
- **Application** (`TaskProcessor.Application`): casos de uso (handlers), DTOs, interfaces de serviços externos.
- **Infrastructure** (`TaskProcessor.Infrastructure`): implementações concretas de repositórios (MongoDB) e mensageria (RabbitMQ/MassTransit).
- **API** (`TaskProcessor.API`): controllers, injeção de dependência, configuração do ASP.NET.
- **Worker** (`TaskProcessor.Worker`): background services, consumers do MassTransit.

---

## Regras de Código

### Geral
- Linguagem: **C# 12**, .NET 8
- Sempre usar **injeção de dependência** via construtor
- Nunca instanciar repositórios ou serviços com `new` fora de testes
- Prefira `record` para DTOs e Value Objects imutáveis
- Use `CancellationToken` em todos os métodos assíncronos
- Todos os métodos assíncronos terminam com sufixo `Async`

### Domain
- Entidades nunca dependem de nada externo (sem using de Infrastructure ou Application)
- Lógica de negócio fica na entidade, não no handler
- Interfaces de repositório ficam em `Domain/Interfaces/`
- Enums ficam em `Domain/Enums/`

```csharp
// ✅ Correto — lógica na entidade
public class Job
{
    public void MarkAsProcessing() => Status = JobStatus.EmProcessamento;
    public void MarkAsDone() => Status = JobStatus.Concluido;
    public void MarkAsFailed() => Status = JobStatus.Erro;
    public bool CanRetry(int maxRetries) => RetryCount < maxRetries;
}

// ❌ Errado — lógica no handler
job.Status = JobStatus.EmProcessamento;
```

### Application
- Handlers recebem um `Command` ou `Query` e retornam um resultado
- Handlers não conhecem MongoDB, RabbitMQ ou qualquer detalhe de infraestrutura
- Dependem apenas de interfaces definidas no próprio Domain ou Application

```csharp
// ✅ Correto
public class CreateJobHandler(IJobRepository repository, IMessagePublisher publisher)
{
    public async Task<Guid> HandleAsync(CreateJobCommand command, CancellationToken ct)
    {
        var job = new Job(command.Type, command.Payload);
        await repository.AddAsync(job, ct);
        await publisher.PublishAsync(job, ct);
        return job.Id;
    }
}
```

### Infrastructure
- Implementações de `IJobRepository` ficam em `Infrastructure/Persistence/`
- Implementações de `IMessagePublisher` ficam em `Infrastructure/Messaging/`
- Usar o driver oficial do MongoDB (`MongoDB.Driver`)
- Usar **MassTransit** para publicar e consumir mensagens — nunca usar RabbitMQ diretamente

```csharp
// ✅ Publicar via MassTransit
public class MassTransitPublisher(IPublishEndpoint publishEndpoint) : IMessagePublisher
{
    public async Task PublishAsync(Job job, CancellationToken ct) =>
        await publishEndpoint.Publish(new JobCreatedMessage(job.Id, job.Type), ct);
}
```

### Controllers (API)
- Controllers são finos: recebem request, chamam handler, retornam response
- Nunca acessar repositório direto no controller
- Retornar `IActionResult` ou `ActionResult<T>`
- Usar `[ProducesResponseType]` para documentar os status codes

```csharp
// ✅ Controller fino
[HttpPost]
[ProducesResponseType(typeof(CreateJobResponse), StatusCodes.Status201Created)]
public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken ct)
{
    var id = await _handler.HandleAsync(new CreateJobCommand(request.Type, request.Payload), ct);
    return CreatedAtAction(nameof(GetById), new { id }, new CreateJobResponse(id));
}
```

### Workers / Consumers
- Consumers ficam em `Worker/Consumers/`
- Herdam de `IConsumer<T>` do MassTransit
- Ao falhar, lançar exceção para o MassTransit gerenciar o retry automaticamente
- Atualizar o status do job no MongoDB antes e depois do processamento

```csharp
public class JobConsumer(IJobRepository repository) : IConsumer<JobCreatedMessage>
{
    public async Task Consume(ConsumeContext<JobCreatedMessage> context)
    {
        var job = await repository.GetByIdAsync(context.Message.JobId, context.CancellationToken);
        job.MarkAsProcessing();
        await repository.UpdateAsync(job, context.CancellationToken);

        // processamento simulado...

        job.MarkAsDone();
        await repository.UpdateAsync(job, context.CancellationToken);
    }
}
```

---

## Nomenclatura

| O que é | Padrão | Exemplo |
|---|---|---|
| Entidade | `NomeSingular` | `Job` |
| Interface | `I + Nome` | `IJobRepository` |
| Handler de criação | `Create + Entidade + Handler` | `CreateJobHandler` |
| Handler de consulta | `Get + Entidade + Handler` | `GetJobHandler` |
| Command / Query | `Ação + Entidade + Command` | `CreateJobCommand` |
| Consumer | `Entidade + Consumer` | `JobConsumer` |
| Mensagem de fila | `Entidade + Evento + Message` | `JobCreatedMessage` |
| DTO de request | `Ação + Entidade + Request` | `CreateJobRequest` |
| DTO de response | `Ação + Entidade + Response` | `CreateJobResponse` |

---

## Estrutura de Pastas

```
src/
├── TaskProcessor.Domain/
│   ├── Entities/Job.cs
│   ├── Enums/JobStatus.cs
│   └── Interfaces/IJobRepository.cs
│
├── TaskProcessor.Application/
│   ├── UseCases/CreateJob/CreateJobHandler.cs
│   ├── UseCases/CreateJob/CreateJobCommand.cs
│   ├── UseCases/GetJob/GetJobHandler.cs
│   └── Interfaces/IMessagePublisher.cs
│
├── TaskProcessor.Infrastructure/
│   ├── Persistence/MongoDbContext.cs
│   ├── Persistence/JobRepository.cs
│   └── Messaging/MassTransitPublisher.cs
│
├── TaskProcessor.API/
│   ├── Controllers/JobsController.cs
│   └── Program.cs
│
└── TaskProcessor.Worker/
    ├── Consumers/JobConsumer.cs
    └── Program.cs
```

---

## O que Nunca Fazer

- ❌ Nunca referenciar `Infrastructure` no `Domain` ou `Application`
- ❌ Nunca usar RabbitMQ diretamente — sempre via MassTransit
- ❌ Nunca acessar `IJobRepository` direto no Controller
- ❌ Nunca colocar lógica de negócio no Consumer ou Controller
- ❌ Nunca usar métodos síncronos onde existe versão `Async`
- ❌ Nunca hardcodar strings de conexão — usar variáveis de ambiente
