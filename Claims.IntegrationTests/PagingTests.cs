using System.Net;
using Claims.Application.Contracts;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PagingTests(ClaimsApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Without_parameters_the_first_page_of_ten_is_returned()
    {
        await _client.CreateCoverAsync();

        var response = await _client.GetAsync("/Covers", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadAsync<PagedResponse<CoverResponse>>();
        Assert.Equal((1, PageRequest.DefaultPageSize), (page.Page, page.PageSize));
        Assert.Equal(Math.Min(page.TotalCount, PageRequest.DefaultPageSize), page.Items.Count);
    }

    [Fact]
    public async Task Consecutive_pages_do_not_overlap()
    {
        await _client.CreateCoverAsync();
        await _client.CreateCoverAsync();

        var first = await (await _client.GetAsync("/Covers?page=1&pageSize=1", CancellationToken))
            .ReadAsync<PagedResponse<CoverResponse>>();
        var second = await (await _client.GetAsync("/Covers?page=2&pageSize=1", CancellationToken))
            .ReadAsync<PagedResponse<CoverResponse>>();

        Assert.NotEqual(Assert.Single(first.Items).Id, Assert.Single(second.Items).Id);
        Assert.Equal(first.TotalCount, second.TotalCount);
    }

    [Fact]
    public async Task Covers_come_from_MongoDB_in_order_of_start_date_then_id()
    {
        await _client.PostCoverAsync(new CreateCoverRequest(ClaimsApiFactory.Today.AddDays(5), ClaimsApiFactory.Today.AddDays(9), CoverType.Tanker));
        await _client.PostCoverAsync(new CreateCoverRequest(ClaimsApiFactory.Today.AddDays(1), ClaimsApiFactory.Today.AddDays(9), CoverType.Tanker));
        await _client.CreateCoverAsync();

        var page = await (await _client.GetAsync($"/Covers?pageSize={PageRequest.MaxPageSize}", CancellationToken))
            .ReadAsync<PagedResponse<CoverResponse>>();

        var expected = page.Items.OrderBy(cover => cover.StartDate).ThenBy(cover => cover.Id, StringComparer.Ordinal);
        Assert.Equal(expected.Select(cover => cover.Id), page.Items.Select(cover => cover.Id));
    }

    // The error is named after the query parameter: model binding already holds an entry for it in ModelState.
    [Theory]
    [InlineData("/Covers?pageSize=101", "pageSize")]
    [InlineData("/Claims?page=0", "page")]
    public async Task A_page_out_of_range_is_a_validation_problem(string url, string parameter)
    {
        var response = await _client.GetAsync(url, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(parameter, (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }
}
