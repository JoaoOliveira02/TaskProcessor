# TaskProcessor — Servico de Processamento de Tarefas

> Desafio tecnico desenvolvido com **C# / ASP.NET Core**, aplicando **DDD**, **Clean Architecture**, **CQRS**, **MongoDB** e **RabbitMQ**.

---

## Indice

- [Visao Geral](#visao-geral)
- [Arquitetura](#arquitetura)
  - [Clean Architecture](#clean-architecture)
  - [DDD — Domain-Driven Design](#ddd--domain-driven-design)
  - [CQRS — Command Query Responsibility Segregation](#cqrs--command-query-responsibility-segregation)
  - [Por que DDD + Clean Architecture + CQRS juntos?](#por-que-ddd--clean-architecture--cqrs-juntos)
- [Tecnologias](#tecnologias)
  - [MongoDB — Por que NoSQL aqui?](#mongodb--por-que-nosql-aqui)
  - [RabbitMQ — Por que mensageria?](#rabbitmq--por-que-mensageria)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Fluxo da Aplicacao](#fluxo-da-aplicacao)
- [Endpoints da API](#endpoints-da-api)
- [Como Rodar](#como-rodar)
- [Variaveis de Ambiente](#variaveis-de-ambiente)

---

## Visao Geral

A aplicacao expoe uma **API REST** para criacao e consulta de tarefas (_jobs_). Cada tarefa e persistida no **MongoDB** e publicada em uma fila do **RabbitMQ**, onde **Workers** em background a consomem de forma assincrona e concorrente.

**Funcionalidades:**
- Criacao de tarefas via API
- Processamento assincrono em background (Workers)
- Controle de status: `Pending -> InProcessing -> Completed / Error`
- Sistema de re-tentativa com limite maximo configuravel (padrao: 3 tentativas)
- Controle de concorrencia com ate 3 Workers simultaneos
- Logs estruturados com tempo de processamento em cada etapa
- Swagger documentado com exemplos de requisicao
- Containerizacao completa com Docker

---

## Arquitetura

### Clean Architecture

A Clean Architecture organiza o codigo em **camadas concentricas**, onde a regra e: **dependencias sempre apontam para dentro**. A camada mais interna (Domain) nao conhece nenhuma outra.

```
┌──────────────────────────────────────┐
│           Infrastructure             │  <- MongoDB, RabbitMQ
│  ┌────────────────────────────────┐  │
│  │         Application            │  │  <- Commands, CQRS, Queries
│  │  ┌──────────────────────────┐  │  │
│  │  │         Domain           │  │  │  <- Entidades, Enums, Interfaces
│  │  └──────────────────────────┘  │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
         ↑
      API / Worker (ponto de entrada)
```

| Camada | Responsabilidade | Depende de |
|---|---|---|
| **Domain** | Entidades, regras de negocio, interfaces | Nada |
| **Application** | Casos de uso, CQRS, orquestracao | Domain |
| **Infrastructure** | MongoDB, RabbitMQ, repositorios concretos | Application + Domain |
| **API / Worker** | Controllers, Background Services | Application |

> **Regra de ouro:** se precisar trocar o MongoDB por PostgreSQL, so a camada de Infrastructure muda. O Domain e a Application nunca sabem qual banco esta sendo usado.

---

### DDD — Domain-Driven Design

O DDD e uma **forma de pensar e modelar** o software centrada no dominio do negocio. Nao e uma arquitetura, e uma filosofia de design.

**Conceitos aplicados neste projeto:**

#### Entidade (_Entity_)
Objeto com **identidade unica** que persiste ao longo do tempo. No nosso caso: `Job`.

```
Job
 ├── Id (GUID)               <- identidade unica
 ├── Type (string)           <- tipo da tarefa
 ├── Payload (JSON)          <- dados para processamento
 ├── Status (enum)           <- estado atual
 ├── RetryCount (int)        <- controle de re-tentativas
 ├── ErrorMessage (string?)  <- mensagem de erro se houver
 └── CreatedAt (DateTime)    <- auditoria
```

A logica de negocio fica **dentro da entidade**, nao espalhada nos handlers:

```csharp
// Sem DDD — logica espalhada no handler
job.Status = JobStatus.Completed;
job.UpdatedAt = DateTime.UtcNow;

// Com DDD — logica encapsulada na entidade
job.MarkAsDone();
```

#### Repositorio (_Repository_)
Interface definida no **Domain** que abstrai o acesso a dados. A implementacao concreta fica na Infrastructure.

```
Domain/Interfaces/IJobRepository.cs          <- contrato (interface)
Infrastructure/Persistence/JobRepository.cs  <- implementacao com MongoDB
```

#### Agregado (_Aggregate_)
`Job` e o agregado raiz — toda operacao passa por ele, garantindo consistencia.

---

### CQRS — Command Query Responsibility Segregation

CQRS separa operacoes de **escrita** (Commands) de operacoes de **leitura** (Queries).

```
Commands -> modificam estado -> CreateJob, DeleteJob
Queries  -> so leem dados   -> GetJob, GetAllJobs, GetJobsByStatus
```

**Interfaces definidas na camada Application:**

```csharp
// Escrita
ICommandHandler<TCommand, TResult>

// Leitura
IQueryHandler<TQuery, TResult>
```

**Beneficio pratico:** o controller depende de interfaces, nao de implementacoes concretas. Facil de testar, facil de evoluir.

```csharp
// Controller injeta pela interface — nao conhece a implementacao
ICommandHandler<CreateJobCommand, Guid> createHandler
IQueryHandler<GetJobQuery, Job?> getHandler
```

---

### Por que DDD + Clean Architecture + CQRS juntos?

Cada um resolve um problema diferente:

| Padrao | Responde |
|---|---|
| **DDD** | *O que modelar* — como representar o dominio do negocio |
| **Clean Architecture** | *Onde colocar* — como organizar camadas e dependencias |
| **CQRS** | *Como separar* — escrita e leitura com responsabilidades distintas |

Juntos garantem que o codigo seja **legivel**, **testavel** e **facil de evoluir**.

---

## Tecnologias

| Tecnologia | Versao | Papel |
|---|---|---|
| C# / ASP.NET Core | .NET 8 | API e Workers |
| MongoDB | 7.x | Persistencia de tarefas |
| RabbitMQ | 3.x | Fila de mensagens |
| MassTransit | 8.2.5 | Abstracao sobre RabbitMQ |
| Swashbuckle | — | Documentacao Swagger |
| Docker + Compose | — | Containerizacao |

---

### MongoDB — Por que NoSQL aqui?

MongoDB e um banco **orientado a documentos**. Cada registro e um documento JSON (internamente BSON).

**Por que faz sentido neste projeto:**

- O campo `Payload` e um JSON livre — cada tipo de tarefa tem dados diferentes. Em SQL seria necessario serializar como string ou criar tabelas dinamicas.
- O schema pode evoluir sem migrations: se `EnviarEmail` ganhar um novo campo, documentos antigos continuam validos.
- Escala horizontalmente para alto volume de tarefas.

```
Banco Relacional   ->   MongoDB
────────────────────────────────
Database           ->   Database
Table              ->   Collection
Row                ->   Document (JSON/BSON)
Primary Key        ->   _id
JOIN               ->   Embed ou $lookup
```

---

### RabbitMQ — Por que mensageria?

RabbitMQ e um **message broker**: intermediario que recebe mensagens de produtores e entrega para consumidores.

**Por que nao processar direto na API?**

Se a API processasse a tarefa na mesma requisicao HTTP:
- O cliente ficaria esperando (bloqueado)
- Um pico de requisicoes travaria o servidor
- Uma falha no processamento afetaria a resposta HTTP

Com RabbitMQ:
```
API (Producer)       RabbitMQ          Worker (Consumer)
     │                  │                     │
     │── publica Job ──▶│                     │
     │◀── 201 Created ──│                     │
     │                  │── entrega Job ──────▶│
     │                  │                     │── processa...
     │                  │                     │── atualiza status
```

**MassTransit** abstrai o RabbitMQ — se quiser trocar por Azure Service Bus no futuro, so muda a configuracao.

---

## Estrutura do Projeto

```
TaskProcessor/
│
├── TaskProcessor.Domain/
│   ├── Entities/
│   │   └── Job.cs                      # Entidade principal com regras de negocio
│   ├── Enums/
│   │   └── JobStatus.cs                # Pending, InProcessing, Completed, Error
│   └── Interfaces/
│       ├── IJobRepository.cs           # Contrato do repositorio
│       └── IMessagePublisher.cs        # Contrato da fila
│
├── TaskProcessor.Application/
│   ├── CQRS/
│   │   ├── ICommandHandler.cs          # Interface para handlers de escrita
│   │   └── IQueryHandler.cs            # Interface para handlers de leitura
│   ├── Commands/
│   │   ├── CreateJobHandler.cs         # Cria um novo job
│   │   └── DeleteJobHandler.cs         # Remove um job
│   └── Queries/
│       ├── GetJobHandler.cs            # Busca job por ID
│       ├── GetAllJobsHandler.cs        # Lista todos os jobs
│       └── GetJobsByStatusHandler.cs   # Filtra jobs por status
│
├── TaskProcessor.Infrastructure/
│   ├── Persistence/
│   │   ├── MongoDbContext.cs           # Configuracao do MongoDB
│   │   └── JobRepository.cs           # Implementacao concreta
│   └── Messaging/
│       ├── JobCreatedMessage.cs        # Contrato da mensagem na fila
│       └── MassTransitPublisher.cs     # Implementacao com MassTransit
│
├── TaskProcessor.API/
│   ├── Controllers/
│   │   └── JobsController.cs
│   ├── Models/                         # DTOs de request e response
│   └── Program.cs
│
├── TaskProcessor.Worker/
│   ├── Consumers/
│   │   └── JobConsumer.cs             # Consome mensagens do RabbitMQ
│   └── Program.cs
│
├── docker-compose.yml
├── Dockerfile
└── README.md
```

---

## Fluxo da Aplicacao

```
1. Cliente faz POST /api/jobs
        ↓
2. JobsController recebe o request
        ↓
3. CreateJobHandler (Application)
   ├── Cria entidade Job com status Pending
   ├── Salva no MongoDB via IJobRepository
   └── Publica mensagem no RabbitMQ via IMessagePublisher
        ↓
4. API retorna 201 Created com o Id do Job
        ↓
5. Worker consome a mensagem do RabbitMQ
   ├── Atualiza status para InProcessing
   ├── Executa o processamento (simulado)
   ├── Atualiza status para Completed
   └── Em caso de erro:
       ├── Incrementa RetryCount
       ├── Se RetryCount < 3: reenfileira (aguarda 5s, 10s, 15s)
       └── Se RetryCount >= 3: status Error
        ↓
6. Cliente faz GET /api/jobs/{id} para consultar o status
```

---

## Endpoints da API

Acesse a documentacao interativa em `http://localhost:5000/swagger`

---

### Criar Tarefa — Fluxo de Sucesso

```http
POST /api/jobs
Content-Type: application/json

{
  "type": "EnviarEmail",
  "payload": "{\"to\": \"usuario@email.com\", \"subject\": \"Bem-vindo!\"}"
}
```

**Response `201 Created`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

Apos alguns segundos, o GET retornara:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "EnviarEmail",
  "status": "Completed",
  "retryCount": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:05Z",
  "errorMessage": null
}
```

---

### Criar Tarefa — Fluxo de Erro com Retry

Para testar o sistema de re-tentativas, use o tipo `ErroForcado`:

```http
POST /api/jobs
Content-Type: application/json

{
  "type": "ErroForcado",
  "payload": "{\"teste\": \"payload de erro forcado\"}"
}
```

O Worker vai executar o seguinte fluxo:

```
Tentativa 1 -> falha -> aguarda 5s  -> reenfileira (RetryCount: 1)
Tentativa 2 -> falha -> aguarda 10s -> reenfileira (RetryCount: 2)
Tentativa 3 -> falha -> aguarda 15s -> reenfileira (RetryCount: 3)
Tentativa 4 -> falha -> limite atingido -> status: Error
```

**Response final do GET:**
```json
{
  "id": "...",
  "type": "ErroForcado",
  "status": "Error",
  "retryCount": 3,
  "errorMessage": "Erro simulado para teste!"
}
```

> Voce pode acompanhar o processo em tempo real no **RabbitMQ Dashboard** em `http://localhost:15672` — va em **Queues and Streams** e observe o grafico da fila `job-queue`.

---

### Consultar Job por ID

```http
GET /api/jobs/{id}
```

---

### Listar Todos os Jobs

```http
GET /api/jobs
```

---

### Filtrar por Status

```http
GET /api/jobs/status/{status}
```

| Valor | Status |
|---|---|
| `0` | Pending |
| `1` | InProcessing |
| `2` | Completed |
| `3` | Error |

---

### Deletar Job

```http
DELETE /api/jobs/{id}
```

**Response `204 No Content`** — job removido com sucesso.

---

## Como Rodar

### Pre-requisitos
- [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/)

### 1. Clone o repositorio
```bash
git clone https://github.com/JoaoOliveira02/TaskProcessor.git
cd TaskProcessor
```

### 2. Suba todos os servicos
```bash
docker-compose up --build
```

Isso ira subir:
- **API** em `http://localhost:5000`
- **Worker** processando em background
- **MongoDB** em `localhost:27017`
- **RabbitMQ** em `localhost:5672` (Management UI: `http://localhost:15672`)

### 3. Acesse o Swagger
```
http://localhost:5000/swagger
```

### Credenciais padrao

| Servico | URL | Usuario | Senha |
|---|---|---|---|
| Swagger | http://localhost:5000/swagger | — | — |
| RabbitMQ Management | http://localhost:15672 | guest | guest |
| MongoDB | localhost:27017 | — | — |

---

## Variaveis de Ambiente

| Variavel | Descricao | Padrao |
|---|---|---|
| `MongoDB__ConnectionString` | String de conexao do MongoDB | `mongodb://localhost:27017` |
| `MongoDB__DatabaseName` | Nome do banco | `TaskProcessorDb` |
| `RabbitMQ__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Username` | Usuario | `guest` |
| `RabbitMQ__Password` | Senha | `guest` |
| `Worker__MaxRetries` | Maximo de re-tentativas por job | `3` |
| `Worker__ConcurrentWorkers` | Quantidade de consumers simultaneos | `3` |
