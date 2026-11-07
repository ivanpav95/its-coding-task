using Claims.Application.Services;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController(ICoverService coverService) : ControllerBase
{
    [HttpPost("compute")]
    public ActionResult ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return Ok(coverService.ComputePremium(startDate, endDate, coverType));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await coverService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cover>> GetAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await coverService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(Cover cover, CancellationToken cancellationToken)
    {
        return Ok(await coverService.CreateAsync(cover, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await coverService.DeleteAsync(id, cancellationToken);
    }
}
