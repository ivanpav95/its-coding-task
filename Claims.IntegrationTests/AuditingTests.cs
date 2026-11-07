using Claims.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuditingTests(ClaimsApiFactory factory)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Creating_and_deleting_writes_audit_rows_in_the_background()
    {
        var cover = await _client.CreateCoverAsync();
        var claim = await _client.CreateClaimAsync(cover.Id);
        await _client.DeleteAsync($"/Claims/{claim.Id}", CancellationToken);
        await _client.DeleteAsync($"/Covers/{cover.Id}", CancellationToken);

        var coverAudits = await WaitForAsync(2, context => context.CoverAudits.Where(audit => audit.CoverId == cover.Id)
            .Select(audit => new { audit.HttpRequestType, audit.Created }));
        var claimAudits = await WaitForAsync(2, context => context.ClaimAudits.Where(audit => audit.ClaimId == claim.Id)
            .Select(audit => new { audit.HttpRequestType, audit.Created }));

        Assert.Equal(["DELETE", "POST"], coverAudits.Select(audit => audit.HttpRequestType).Order());
        Assert.Equal(["DELETE", "POST"], claimAudits.Select(audit => audit.HttpRequestType).Order());
        Assert.All(coverAudits.Concat(claimAudits), audit => Assert.Equal(ClaimsApiFactory.Now, audit.Created));
    }

    /// <summary>Audits are saved asynchronously, so poll until the expected rows are there.</summary>
    private async Task<List<T>> WaitForAsync<T>(int count, Func<AuditContext, IQueryable<T>> query)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var rows = await query(scope.ServiceProvider.GetRequiredService<AuditContext>()).ToListAsync(CancellationToken);
            if (rows.Count >= count || DateTime.UtcNow > deadline)
            {
                return rows;
            }

            await Task.Delay(100, CancellationToken);
        }
    }
}
