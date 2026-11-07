using Claims.Application.Contracts;
using Claims.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController(IClaimService claimService) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ClaimResponse>> GetAsync(CancellationToken cancellationToken)
    {
        return await claimService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<ClaimResponse>> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        return Ok(await claimService.CreateAsync(request, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await claimService.DeleteAsync(id, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ClaimResponse?> GetAsync(string id, CancellationToken cancellationToken)
    {
        return await claimService.GetByIdAsync(id, cancellationToken);
    }
}
