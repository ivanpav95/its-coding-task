using Claims.Application.Contracts;
using Claims.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

/// <summary>Insurance claims, each linked to a cover.</summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    /// <summary>Lists all claims.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClaimResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClaimResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await claimService.GetAllAsync(cancellationToken));
    }

    /// <summary>Gets one claim.</summary>
    /// <response code="404">No claim with this id exists.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var claim = await claimService.GetByIdAsync(id, cancellationToken);
        return claim is null ? NotFound() : claim;
    }

    /// <summary>Creates a claim for an existing cover. The id is set by the server.</summary>
    /// <response code="201">The claim was created; <c>Location</c> points to it.</response>
    /// <response code="400">The request is invalid, e.g. the cover doesn't exist.</response>
    [HttpPost]
    [ProducesResponseType<ClaimResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponse>> Create(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        return await claimService.CreateAsync(request, cancellationToken) switch
        {
            CreateClaimResult.Created created =>
                CreatedAtAction(nameof(GetById), new { id = created.Claim.Id }, created.Claim),
            CreateClaimResult.CoverNotFound notFound => CoverDoesNotExist(notFound.CoverId),
            var result => throw new InvalidOperationException($"Unhandled result {result}.")
        };
    }

    private ActionResult CoverDoesNotExist(string coverId)
    {
        ModelState.AddModelError(nameof(CreateClaimRequest.CoverId), $"Cover '{coverId}' does not exist.");
        return ValidationProblem(ModelState);
    }

    /// <summary>Deletes a claim.</summary>
    /// <response code="204">The claim was deleted.</response>
    /// <response code="404">No claim with this id exists.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return await claimService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
