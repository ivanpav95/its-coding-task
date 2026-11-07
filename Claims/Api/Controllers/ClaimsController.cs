using Claims.Application.Services;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<Claim>> GetAsync(CancellationToken cancellationToken)
    {
        return await claimService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(Claim claim, CancellationToken cancellationToken)
    {
        return Ok(await claimService.CreateAsync(claim, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await claimService.DeleteAsync(id, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<Claim?> GetAsync(string id, CancellationToken cancellationToken)
    {
        return await claimService.GetByIdAsync(id, cancellationToken);
    }
}
