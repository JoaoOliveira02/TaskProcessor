# 🚀 TaskProcessor — Serviço de Processamento de Tarefas

> Desafio técnico desenvolvido com **C# / ASP.NET Core**, aplicando **DDD**, **Clean Architecture**, **MongoDB** e **RabbitMQ**.

---

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
  - [Clean Architecture](#clean-architecture)
  - [DDD — Domain-Driven Design](#ddd--domain-driven-design)
  - [Por que DDD + Clean Architecture juntos?](#por-que-ddd--clean-architecture-juntos)
- [Tecnologias](#tecnologias)
  - [MongoDB — Por que NoSQL aqui?](#mongodb--por-que-nosql-aqui)
  - [RabbitMQ — Por que mensageria?](#rabbitmq--por-que-mensageria)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Fluxo da Aplicação](#fluxo-da-aplicação)
- [Endpoints da API](#endpoints-da-api)
- [Como Rodar](#como-rodar)
- [Variáveis de Ambiente](#variáveis-de-ambiente)

---

## Visão Geral

A aplicação expõe uma **API REST** para criação e consulta de tarefas (_jobs_). Cada tarefa é persistida no **MongoDB** e publicada em uma fila do **RabbitMQ**, onde **Workers** em background a consomem de forma assíncrona e concorrente.

**Funcionalidades:**
- Criação de tarefas via API
- Processamento assíncrono em background (Workers)
- Controle de status: `Pendente → EmProcessamento → Concluido / Erro`
- Sistema de re-tentativa com limite máximo configurável
- Controle de concorrência entre múltiplos Workers
- Containerização com Docker

---

## Arquitetura

### Clean Architecture

A Clean Architecture organiza o código em **camadas concêntricas**, onde a regra é: **dependências sempre apontam para dentro**. A camada mais interna (Domain) não conhece nenhuma outra.

```
┌──────────────────────────────────────┐
│           Infrastructure             │  ← MongoDB, RabbitMQ, EF, etc.
│  ┌────────────────────────────────┐  │
│  │         Application            │  │  ← Casos de uso, DTOs, interfaces
│  │  ┌──────────────────────────┐  │  │
│  │  │         Domain           │  │  │  ← Entidades, Value Objects, Regras
│  │  └──────────────────────────┘  │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
         ↑
      API / Workers (ponto de entrada)
```

| Camada | Responsabilidade | Depende de |
|---|---|---|
| **Domain** | Entidades, regras de negócio, interfaces | Nada |
| **Application** | Casos de uso (UseCases), DTOs, orquestração | Domain |
| **Infrastructure** | MongoDB, RabbitMQ, repositórios concretos | Application + Domain |
| **API / Worker** | Controllers, Background Services | Application |

> **Regra de ouro:** se você precisar trocar o MongoDB por PostgreSQL, só a camada de Infrastructure muda. O Domain e a Application nunca sabem qual banco está sendo usado.

---

### DDD — Domain-Driven Design

O DDD é uma **forma de pensar e modelar** o software centrada no domínio do negócio. Não é uma arquitetura, é uma filosofia de design.

**Conceitos aplicados neste projeto:**

#### Entidade (_Entity_)
Objeto com **identidade única** que persiste ao longo do tempo. No nosso caso: `Job`.

```
Job
 ├── Id (GUID)               ← identidade
 ├── Type (string)           ← tipo da tarefa
 ├── Payload (JSON)          ← dados para processamento
 ├── Status (enum)           ← estado atual
 ├── RetryCount (int)        ← controle de re-tentativas
 └── CreatedAt (DateTime)    ← auditoria
```

#### Value Object
Objeto sem identidade própria, definido pelos seus **valores**. Exemplo: `JobStatus` como enum ou tipo fortemente tipado.

#### Repositório (_Repository_)
Interface definida no **Domain** que abstrai o acesso a dados. A implementação concreta fica na Infrastructure.

```
Domain/Interfaces/IJobRepository.cs    ← contrato (interface)
Infrastructure/Repositories/JobRepository.cs  ← implementação com MongoDB
```

#### Serviço de Domínio (_Domain Service_)
Lógica de negócio que não pertence a uma entidade específica. Exemplo: regras de re-tentativa.

#### Agregado (_Aggregate_)
Um agrupamento de entidades/value objects tratado como **uma unidade de consistência**. Aqui, `Job` é o agregado raiz.

---

### Por que DDD + Clean Architecture juntos?

São complementares, não concorrentes:

- **DDD** responde *o quê modelar* — como o domínio do negócio deve ser representado
- **Clean Architecture** responde *onde colocar* — como organizar as camadas e dependências

Juntos garantem que o código seja **legível**, **testável** e **fácil de evoluir**.

---

## Tecnologias

| Tecnologia | Versão | Papel |
|---|---|---|
| C# / ASP.NET Core | .NET 8 | API e Workers |
| MongoDB | 7.x | Persistência de tarefas |
| RabbitMQ | 3.x | Fila de mensagens |
| MassTransit | 8.x | Abstração sobre RabbitMQ |
| Docker + Compose | — | Containerização |

---

### MongoDB — Por que NoSQL aqui?

MongoDB é um banco **orientado a documentos**. Cada registro é um documento JSON (internamente BSON).

**Por que faz sentido neste projeto:**

- Cada `Job` carrega um campo `Payload` que é JSON livre — em SQL você teria que serializar como string ou criar tabelas dinâmicas. No MongoDB, você simplesmente armazena o objeto.
- O schema pode evoluir sem migrations: se amanhã `EnviarEmail` ganhar um novo campo, documentos antigos continuam válidos.
- Escala horizontalmente com facilidade para alto volume de tarefas.

**Conceitos básicos do MongoDB:**

```
Banco Relacional   →   MongoDB
─────────────────────────────
Database           →   Database
Table              →   Collection
Row                →   Document (JSON/BSON)
Column             →   Field
Primary Key        →   _id
JOIN               →   Embed ou $lookup
```

**Exemplo de documento `Job` no MongoDB:**
```json
{
  "_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "EnviarEmail",
  "payload": {
    "to": "user@example.com",
    "subject": "Bem-vindo!"
  },
  "status": "Pendente",
  "retryCount": 0,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

---

### RabbitMQ — Por que mensageria?

RabbitMQ é um **message broker**: um intermediário que recebe mensagens de produtores (_publishers_) e entrega para consumidores (_consumers_).

**Por que não processar direto na API?**

Se a API processasse a tarefa na mesma requisição HTTP:
- O cliente ficaria esperando (bloqueado)
- Um pico de 1000 requisições travaria o servidor
- Uma falha no processamento afetaria a resposta HTTP

Com RabbitMQ:
```
API (Producer)          RabbitMQ            Worker (Consumer)
     │                     │                      │
     │── publica Job ──────▶│                      │
     │◀── 201 Created ──────│                      │
     │                     │── entrega Job ───────▶│
     │                     │                      │── processa...
     │                     │                      │── atualiza status
```

**Conceitos básicos:**

| Conceito | O que é |
|---|---|
| **Exchange** | Recebe a mensagem e decide para qual fila enviar |
| **Queue** | Fila onde as mensagens ficam aguardando |
| **Binding** | Regra que liga Exchange à Queue |
| **Consumer** | Quem lê e processa as mensagens da fila |
| **Ack / Nack** | Confirmação de que a mensagem foi processada (ou não) |

**MassTransit** é uma biblioteca .NET que abstrai o RabbitMQ, deixando o código agnóstico ao broker. Se quiser trocar por Azure Service Bus no futuro, só muda a configuração.

---

## Estrutura do Projeto

```
TaskProcessor/
│
├── src/
│   ├── TaskProcessor.Domain/            # Camada de Domínio
│   │   ├── Entities/
│   │   │   └── Job.cs                   # Entidade principal
│   │   ├── Enums/
│   │   │   └── JobStatus.cs             # Status possíveis
│   │   └── Interfaces/
│   │       └── IJobRepository.cs        # Contrato do repositório
│   │
│   ├── TaskProcessor.Application/       # Camada de Aplicação
│   │   ├── UseCases/
│   │   │   ├── CreateJob/
│   │   │   │   ├── CreateJobCommand.cs  # DTO de entrada
│   │   │   │   └── CreateJobHandler.cs  # Caso de uso
│   │   │   └── GetJob/
│   │   │       └── GetJobHandler.cs
│   │   └── Interfaces/
│   │       └── IMessagePublisher.cs     # Contrato da fila
│   │
│   ├── TaskProcessor.Infrastructure/    # Camada de Infraestrutura
│   │   ├── Persistence/
│   │   │   ├── MongoDbContext.cs        # Configuração do MongoDB
│   │   │   └── JobRepository.cs        # Implementação concreta
│   │   └── Messaging/
│   │       └── RabbitMqPublisher.cs     # Implementação da fila
│   │
│   ├── TaskProcessor.API/               # Ponto de entrada da API
│   │   ├── Controllers/
│   │   │   └── JobsController.cs
│   │   └── Program.cs
│   │
│   └── TaskProcessor.Worker/            # Background Service (Consumer)
│       ├── Consumers/
│       │   └── JobConsumer.cs           # Processa mensagens do RabbitMQ
│       └── Program.cs
│
├── docker-compose.yml
├── Dockerfile
└── README.md
```

---

## Fluxo da Aplicação

```
1. Cliente faz POST /jobs
        ↓
2. JobsController recebe o request
        ↓
3. CreateJobHandler (Application)
   ├── Cria entidade Job com status "Pendente"
   ├── Salva no MongoDB via IJobRepository
   └── Publica mensagem no RabbitMQ via IMessagePublisher
        ↓
4. API retorna 201 Created com o Id do Job
        ↓
5. Worker consome a mensagem do RabbitMQ
   ├── Atualiza status para "EmProcessamento" no MongoDB
   ├── Executa o processamento (simulado)
   ├── Atualiza status para "Concluido"
   └── Em caso de erro: incrementa RetryCount
       ├── Se RetryCount < MaxRetries: republica na fila
       └── Se RetryCount >= MaxRetries: status "Erro"
        ↓
6. Cliente faz GET /jobs/{id} para consultar o status
```

---

## Endpoints da API

### Criar Tarefa
```http
POST /api/jobs
Content-Type: application/json

{
  "type": "EnviarEmail",
  "payload": {
    "to": "user@example.com",
    "subject": "Bem-vindo ao sistema"
  }
}
```

**Response `201 Created`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "EnviarEmail",
  "status": "Pendente",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### Consultar Status
```http
GET /api/jobs/{id}
```

**Response `200 OK`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "EnviarEmail",
  "status": "Concluido",
  "retryCount": 0,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:05Z"
}
```

**Status possíveis:** `Pendente` | `EmProcessamento` | `Concluido` | `Erro`

---

## Como Rodar

### Pré-requisitos
- [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/)

### 1. Clone o repositório
```bash
git clone https://github.com/seu-usuario/task-processor.git
cd task-processor
```

### 2. Suba todos os serviços
```bash
docker-compose up --build
```

Isso irá subir:
- **API** em `http://localhost:5000`
- **Worker** processando em background
- **MongoDB** em `localhost:27017`
- **RabbitMQ** em `localhost:5672` (Management UI: `http://localhost:15672`)

### 3. Teste a API
```bash
# Criar uma tarefa
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{"type": "EnviarEmail", "payload": {"to": "test@example.com"}}'

# Consultar o status (substitua pelo ID retornado)
curl http://localhost:5000/api/jobs/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### Credenciais padrão
| Serviço | URL | Usuário | Senha |
|---|---|---|---|
| RabbitMQ Management | http://localhost:15672 | guest | guest |
| MongoDB | localhost:27017 | — | — |

---

## Variáveis de Ambiente

Configuradas via `docker-compose.yml` ou `appsettings.json`:

| Variável | Descrição | Padrão |
|---|---|---|
| `MongoDB__ConnectionString` | String de conexão do MongoDB | `mongodb://localhost:27017` |
| `MongoDB__DatabaseName` | Nome do banco | `TaskProcessorDb` |
| `RabbitMQ__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Username` | Usuário | `guest` |
| `RabbitMQ__Password` | Senha | `guest` |
| `Worker__MaxRetries` | Máximo de re-tentativas por job | `3` |
| `Worker__ConcurrentWorkers` | Quantidade de consumers simultâneos | `3` |
