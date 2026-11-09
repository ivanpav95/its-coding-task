using System.Net;
using Claims.Application.Contracts;
using Claims.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ErrorHandlingTests(ClaimsApiFactory factory)
{
    private const string InternalDetail = "Connection to the claims database failed";

    [Fact]
    public async Task An_unexpected_error_is_a_500_problem_without_internal_details()
    {
        await using var failingApp = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped<ICoverService, FailingCoverService>()));

        var response = await failingApp.CreateClient().GetAsync("/Covers", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(InternalDetail, body);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Equal(500, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    private sealed class FailingCoverService : ICoverService
    {
        public Task<IReadOnlyList<CoverResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(InternalDetail);

        public Task<CoverResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(InternalDetail);

        public Task<CreateCoverResult> CreateAsync(CreateCoverRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(InternalDetail);

        public Task<DeleteCoverResult> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(InternalDetail);

        public Task<ComputePremiumResult> ComputePremiumAsync(CreateCoverRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(InternalDetail);
    }
}
