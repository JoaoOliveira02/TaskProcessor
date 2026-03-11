using TaskProcessor.Domain.Entities;

namespace TaskProcessor.API.Models;

/// <summary>
/// Resposta com os dados de um job
/// </summary>
public record JobResponse
{
    /// <summary>
    /// Identificador único do job
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; }

    /// <summary>
    /// Tipo do job
    /// </summary>
    /// <example>ProcessPayment</example>
    public string Type { get; init; }

    /// <summary>
    /// Dados do job em formato JSON
    /// </summary>
    /// <example>{"amount": 100, "currency": "BRL"}</example>
    public string Payload { get; init; }

    /// <summary>
    /// Status atual do job (Pending, InProcessing, Completed, Error)
    /// </summary>
    /// <example>Pending</example>
    public string Status { get; init; }

    /// <summary>
    /// Número de tentativas de reprocessamento
    /// </summary>
    /// <example>0</example>
    public int RetryCount { get; init; }

    /// <summary>
    /// Data e hora de criação do job (UTC)
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Data e hora da última atualização do job (UTC)
    /// </summary>
    /// <example>2024-01-15T10:35:00Z</example>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Mensagem de erro caso o job tenha falhado
    /// </summary>
    /// <example>Timeout ao processar pagamento</example>
    public string? ErrorMessage { get; init; }

    public JobResponse(
        Guid id,
        string type,
        string payload,
        string status,
        int retryCount,
        DateTime createdAt,
        DateTime? updatedAt,
        string? errorMessage)
    {
        Id = id;
        Type = type;
        Payload = payload;
        Status = status;
        RetryCount = retryCount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Converte uma entidade Job em JobResponse
    /// </summary>
    public static JobResponse FromEntity(Job job) => new(
        job.Id,
        job.Type,
        job.Payload,
        job.Status.ToString(),
        job.RetryCount,
        job.CreatedAt,
        job.UpdatedAt,
        job.ErrorMessage);
}
