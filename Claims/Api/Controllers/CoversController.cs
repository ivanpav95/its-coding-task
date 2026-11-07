using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController(ICoverService coverService) : ControllerBase
{
    [HttpPost("compute")]
    public ActionResult<decimal> ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
    {
        return Ok(coverService.ComputePremium(startDate, endDate, coverType));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CoverResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await coverService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CoverResponse>> GetAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await coverService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CoverResponse>> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken)
    {
        return Ok(await coverService.CreateAsync(request, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await coverService.DeleteAsync(id, cancellationToken);
    }
}
