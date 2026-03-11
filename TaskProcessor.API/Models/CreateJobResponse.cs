namespace TaskProcessor.API.Models;

/// <summary>
/// Resposta da criação de um job
/// </summary>
public record CreateJobResponse
{
    /// <summary>
    /// Identificador único do job criado
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; }

    public CreateJobResponse(Guid id)
    {
        Id = id;
    }
}
