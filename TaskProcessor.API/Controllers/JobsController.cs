using Microsoft.AspNetCore.Mvc;
using TaskProcessor.API.Models;
using TaskProcessor.Application.Commands;
using TaskProcessor.Application.CQRS;
using TaskProcessor.Application.Queries;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Enums;

namespace TaskProcessor.API.Controllers;

/// <summary>
/// Gerenciamento de jobs assíncronos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class JobsController(
    ICommandHandler<CreateJobCommand, Guid> createHandler,
    IQueryHandler<GetJobQuery, Job?> getHandler,
    IQueryHandler<GetAllJobsQuery, IEnumerable<Job>> getAllHandler,
    IQueryHandler<GetJobsByStatusQuery, IEnumerable<Job>> getByStatusHandler,
    ICommandHandler<DeleteJobCommand> deleteHandler) : ControllerBase
{
    /// <summary>
    /// Cria um novo job para processamento assíncrono
    /// </summary>
    /// <param name="request">Dados do job a ser criado</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>ID do job criado</returns>
    /// <response code="201">Job criado com sucesso</response>
    /// <response code="400">Dados inválidos na requisição</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Payload))
            return BadRequest(new { error = "Type e Payload são obrigatórios." });

        var id = await createHandler.HandleAsync(new CreateJobCommand(request.Type, request.Payload), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new CreateJobResponse(id));
    }

    /// <summary>
    /// Busca um job específico por ID
    /// </summary>
    /// <param name="id">Identificador único do job</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Dados do job</returns>
    /// <response code="200">Job encontrado</response>
    /// <response code="404">Job não encontrado</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var job = await getHandler.HandleAsync(new GetJobQuery(id), ct);
        if (job is null) return NotFound(new { error = $"Job com ID {id} não encontrado." });
        return Ok(JobResponse.FromEntity(job));
    }

    /// <summary>
    /// Lista todos os jobs cadastrados
    /// </summary>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Lista de jobs</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<JobResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var jobs = await getAllHandler.HandleAsync(new GetAllJobsQuery(), ct);
        return Ok(jobs.Select(JobResponse.FromEntity));
    }

    /// <summary>
    /// Filtra jobs por status
    /// </summary>
    /// <param name="status">Status do job (0=Pending, 1=InProcessing, 2=Completed, 3=Error)</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Lista de jobs com o status especificado</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="400">Status inválido</response>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<JobResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStatus(JobStatus status, CancellationToken ct)
    {
        var jobs = await getByStatusHandler.HandleAsync(new GetJobsByStatusQuery(status), ct);
        return Ok(jobs.Select(JobResponse.FromEntity));
    }

    /// <summary>
    /// Remove um job
    /// </summary>
    /// <param name="id">Identificador único do job</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Job removido com sucesso</response>
    /// <response code="404">Job não encontrado</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await deleteHandler.HandleAsync(new DeleteJobCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}