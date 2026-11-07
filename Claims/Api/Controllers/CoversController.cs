using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Claims.Api.Controllers;

/// <summary>Insurance covers: the insured object, its insurance period and premium.</summary>
[ApiController]
[Route("[controller]")]
public class CoversController(ICoverService coverService) : ControllerBase
{
    /// <summary>Calculates the premium for a cover without creating it.</summary>
    /// <remarks>The same rules apply as when creating a cover.</remarks>
    /// <param name="startDate">First day of the insurance period.</param>
    /// <param name="endDate">Last day of the insurance period.</param>
    /// <param name="coverType">Type of the insured object.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="400">The insurance period is invalid.</response>
    [HttpGet("compute")]
    [ProducesResponseType<decimal>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<decimal>> ComputePremium(
        DateOnly startDate, DateOnly endDate, CoverType coverType, CancellationToken cancellationToken)
    {
        var request = new CreateCoverRequest(startDate, endDate, coverType);
        return await coverService.ComputePremiumAsync(request, cancellationToken) switch
        {
            ComputePremiumResult.Computed computed => computed.Premium,
            ComputePremiumResult.Invalid invalid => this.ValidationProblem(invalid.Errors),
            var result => throw new InvalidOperationException($"Unhandled result {result}.")
        };
    }

    /// <summary>Lists all covers.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CoverResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<ActionResult<IReadOnlyList<CoverResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await coverService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets one cover.</summary>
    /// <response code="404">No cover with this id exists.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<CoverResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CoverResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var cover = await coverService.GetByIdAsync(id, cancellationToken);
        return cover is null ? NotFound() : cover;
    }

    /// <summary>Creates a cover. The id and the premium are set by the server.</summary>
    /// <remarks>
    /// The start date can't be in the past (today, in UTC, is allowed). Both dates are part of the period,
    /// which can be at most one calendar year.
    /// </remarks>
    /// <response code="201">The cover was created; <c>Location</c> points to it.</response>
    /// <response code="400">The request is invalid.</response>
    [HttpPost]
    [ProducesResponseType<CoverResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CoverResponse>> Create(CreateCoverRequest request, CancellationToken cancellationToken)
    {
        return await coverService.CreateAsync(request, cancellationToken) switch
        {
            CreateCoverResult.Created created =>
                CreatedAtAction(nameof(GetById), new { id = created.Cover.Id }, created.Cover),
            CreateCoverResult.Invalid invalid => this.ValidationProblem(invalid.Errors),
            var result => throw new InvalidOperationException($"Unhandled result {result}.")
        };
    }

    /// <summary>Deletes a cover. A cover that still has claims can't be deleted.</summary>
    /// <response code="204">The cover was deleted.</response>
    /// <response code="404">No cover with this id exists.</response>
    /// <response code="409">The cover still has claims; delete them first.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return await coverService.DeleteAsync(id, cancellationToken) switch
        {
            DeleteCoverResult.Deleted => NoContent(),
            DeleteCoverResult.NotFound => NotFound(),
            DeleteCoverResult.HasClaims => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cover has claims",
                detail: $"Cover '{id}' still has claims. Delete them before deleting the cover."),
            var result => throw new InvalidOperationException($"Unhandled result {result}.")
        };
    }
}
