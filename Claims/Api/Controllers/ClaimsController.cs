using Claims.Domain;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;


namespace Claims.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly ILogger<ClaimsController> _logger;
        private readonly ClaimsContext _claimsContext;
        private readonly Auditer _auditer;

        public ClaimsController(ILogger<ClaimsController> logger, ClaimsContext claimsContext, AuditContext auditContext)
        {
            _logger = logger;
            _claimsContext = claimsContext;
            _auditer = new Auditer(auditContext);
        }

        [HttpGet]
        public async Task<IEnumerable<Claim>> GetAsync()
        {
            return await _claimsContext.GetClaimsAsync();
        }

        [HttpPost]
        public async Task<ActionResult> CreateAsync(Claim claim)
        {
            claim.Id = Guid.NewGuid().ToString();
            await _claimsContext.AddItemAsync(claim);
            _auditer.AuditClaim(claim.Id, "POST");
            return Ok(claim);
        }

        [HttpDelete("{id}")]
        public async Task DeleteAsync(string id)
        {
            _auditer.AuditClaim(id, "DELETE");
            await _claimsContext.DeleteItemAsync(id);
        }

        [HttpGet("{id}")]
        public async Task<Claim> GetAsync(string id)
        {
            return await _claimsContext.GetClaimAsync(id);
        }
    }
}
