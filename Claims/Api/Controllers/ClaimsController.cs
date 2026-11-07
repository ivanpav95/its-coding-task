using Claims.Application.Auditing;
using Claims.Application.Repositories;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;


namespace Claims.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly ILogger<ClaimsController> _logger;
        private readonly IClaimRepository _claims;
        private readonly IAuditer _auditer;

        public ClaimsController(ILogger<ClaimsController> logger, IClaimRepository claims, IAuditer auditer)
        {
            _logger = logger;
            _claims = claims;
            _auditer = auditer;
        }

        [HttpGet]
        public async Task<IEnumerable<Claim>> GetAsync()
        {
            return await _claims.GetAllAsync();
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync(Claim claim)
        {
            claim.Id = Guid.NewGuid().ToString();
            await _claims.AddAsync(claim);
            _auditer.AuditClaim(claim.Id, "POST");
            return Ok(claim);
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            _auditer.AuditClaim(id, "DELETE");
            await _claims.DeleteAsync(id);
        }

        [HttpGet("{id}")]
        public async Task<Claim?> GetAsync(string id)
        {
            return await _claims.GetByIdAsync(id);
        }
    }
}
