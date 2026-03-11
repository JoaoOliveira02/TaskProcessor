using System.ComponentModel.DataAnnotations;

namespace TaskProcessor.API.Models;

/// <summary>
/// Requisição para criação de um novo job
/// </summary>
public record CreateJobRequest
{
    /// <summary>
    /// Tipo do job a ser processado (ex: "ProcessPayment", "SendEmail")
    /// </summary>
    /// <example>ProcessPayment</example>
    [Required(ErrorMessage = "O tipo do job é obrigatório")]
    public required string Type { get; init; }

    /// <summary>
    /// Dados do job em formato JSON
    /// </summary>
    /// <example>{"amount": 100, "currency": "BRL"}</example>
    [Required(ErrorMessage = "O payload do job é obrigatório")]
    public required string Payload { get; init; }
}
