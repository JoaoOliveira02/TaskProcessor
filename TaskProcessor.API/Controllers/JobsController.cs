using Microsoft.AspNetCore.Mvc;
using TaskProcessor.Application.UseCases.CreateJob;
using TaskProcessor.Application.UseCases.DeleteJob;
using TaskProcessor.Application.UseCases.GetAllJobs;
using TaskProcessor.Application.UseCases.GetJob;
using TaskProcessor.Application.UseCases.GetJobsByStatus;
using TaskProcessor.Application.UseCases.UpdateJob;
using TaskProcessor.Domain.Entities;
using TaskProcessor.Domain.Enums;

namespace TaskProcessor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(
    CreateJobHandler createHandler,
    GetJobHandler getHandler,
    GetAllJobsHandler getAllHandler,
    GetJobsByStatusHandler getByStatusHandler,
    UpdateJobHandler updateHandler,
    DeleteJobHandler deleteHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Payload))
            return BadRequest("Type e Payload são obrigatórios.");

        var id = await createHandler.HandleAsync(new CreateJobCommand(request.Type, request.Payload), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new CreateJobResponse(id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var job = await getHandler.HandleAsync(new GetJobQuery(id), ct);
        if (job is null) return NotFound();
        return Ok(JobResponse.FromEntity(job));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<JobResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var jobs = await getAllHandler.HandleAsync(new GetAllJobsQuery(), ct);
        return Ok(jobs.Select(JobResponse.FromEntity));
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<JobResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStatus(JobStatus status, CancellationToken ct)
    {
        var jobs = await getByStatusHandler.HandleAsync(new GetJobsByStatusQuery(status), ct);
        return Ok(jobs.Select(JobResponse.FromEntity));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, CancellationToken ct)
    {
        try
        {
            await updateHandler.HandleAsync(new UpdateJobCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

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
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}

public record CreateJobRequest(string Type, string Payload);

public record CreateJobResponse(Guid Id);

public record JobResponse(
    Guid Id,
    string Type,
    string Payload,
    string Status,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? ErrorMessage)
{
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