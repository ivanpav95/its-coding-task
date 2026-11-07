using Claims.Application.Contracts;
using Claims.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Claims.Api.Controllers;

/// <summary>Insurance claims, each linked to a cover.</summary>
[ApiController]
[Route("[controller]")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    /// <summary>Lists all claims.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClaimResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<ActionResult<IReadOnlyList<ClaimResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await claimService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets one claim.</summary>
    /// <response code="404">No claim with this id exists.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClaimResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var claim = await claimService.GetByIdAsync(id, cancellationToken);
        return claim is null ? NotFound() : claim;
    }

    /// <summary>Creates a claim for an existing cover. The id is set by the server.</summary>
    /// <response code="201">The claim was created; <c>Location</c> points to it.</response>
    /// <remarks>
    /// The damage cost must be more than 0 and at most 100,000, and the created date must lie within the
    /// cover's period (both ends included).
    /// </remarks>
    /// <response code="400">The request is invalid, the cover doesn't exist, or the date is outside the cover period.</response>
    [HttpPost]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClaimResponse>> Create(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        return await claimService.CreateAsync(request, cancellationToken) switch
        {
            CreateClaimResult.Created created =>
                CreatedAtAction(nameof(GetById), new { id = created.Claim.Id }, created.Claim),
            CreateClaimResult.Invalid invalid => this.ValidationProblem(invalid.Errors),
            var result => throw new InvalidOperationException($"Unhandled result {result}.")
        };
    }

    /// <summary>Deletes a claim.</summary>
    /// <response code="204">The claim was deleted.</response>
    /// <response code="404">No claim with this id exists.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return await claimService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
