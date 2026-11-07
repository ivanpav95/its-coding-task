using System.Net;
using Claims.Application.Contracts;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CoversEndpointTests(ClaimsApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_created_cover_can_be_read_from_its_Location()
    {
        var response = await _client.PostCoverAsync(
            new CreateCoverRequest(ClaimsApiFactory.Today, ClaimsApiFactory.Today.AddDays(9), CoverType.Yacht));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<CoverResponse>();
        Assert.Equal(13_750m, created.Premium); // 10 days at the Yacht day rate of 1,375

        var read = await _client.GetAsync(response.Headers.Location, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(created, await read.ReadAsync<CoverResponse>());
    }

    [Fact]
    public async Task An_invalid_cover_is_a_validation_problem()
    {
        var response = await _client.PostCoverAsync(
            new CreateCoverRequest(ClaimsApiFactory.Today.AddDays(-1), ClaimsApiFactory.Today, CoverType.Yacht));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateCoverRequest.StartDate), problem.Errors.Keys);
    }

    [Fact]
    public async Task A_cover_can_only_be_deleted_after_its_claims()
    {
        var cover = await _client.CreateCoverAsync();
        var claim = await _client.CreateClaimAsync(cover.Id);

        var conflict = await _client.DeleteAsync($"/Covers/{cover.Id}", CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/Claims/{claim.Id}", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/Covers/{cover.Id}", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/Covers/{cover.Id}", CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Computing_a_premium_is_a_GET()
    {
        var response = await _client.GetAsync(
            $"/Covers/compute?startDate={ClaimsApiFactory.Today:yyyy-MM-dd}&endDate={ClaimsApiFactory.Today.AddDays(9):yyyy-MM-dd}&coverType=Yacht",
            CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(13_750m, await response.ReadAsync<decimal>());
    }
}
