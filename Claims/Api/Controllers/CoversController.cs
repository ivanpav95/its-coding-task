using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

/// <summary>Insurance covers: the insured object, its insurance period and premium.</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class CoversController(ICoverService coverService) : ControllerBase
{
    /// <summary>Calculates the premium for a cover without creating it.</summary>
    /// <param name="startDate">First day of the insurance period.</param>
    /// <param name="endDate">Last day of the insurance period.</param>
    /// <param name="coverType">Type of the insured object.</param>
    [HttpGet("compute")]
    [ProducesResponseType<decimal>(StatusCodes.Status200OK)]
    public ActionResult<decimal> ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        return coverService.ComputePremium(startDate, endDate, coverType);
    }

    /// <summary>Lists all covers.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CoverResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoverResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await coverService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets one cover.</summary>
    /// <response code="404">No cover with this id exists.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<CoverResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoverResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var cover = await coverService.GetByIdAsync(id, cancellationToken);
        return cover is null ? NotFound() : cover;
    }

    /// <summary>Creates a cover. The id and the premium are set by the server.</summary>
    /// <response code="201">The cover was created; <c>Location</c> points to it.</response>
    [HttpPost]
    [ProducesResponseType<CoverResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CoverResponse>> Create(CreateCoverRequest request, CancellationToken cancellationToken)
    {
        var cover = await coverService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = cover.Id }, cover);
    }

    /// <summary>Deletes a cover. A cover that still has claims can't be deleted.</summary>
    /// <response code="204">The cover was deleted.</response>
    /// <response code="404">No cover with this id exists.</response>
    /// <response code="409">The cover still has claims; delete them first.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
