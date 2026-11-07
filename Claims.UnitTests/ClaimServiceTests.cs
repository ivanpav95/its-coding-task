using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Application.Validation;
using Claims.Domain;
using Claims.UnitTests.Fakes;
using Xunit;

namespace Claims.UnitTests;

public class ClaimServiceTests
{
    private const string CoverId = "cover-1";
    private static readonly DateOnly CoverStart = new(2027, 2, 1);
    private static readonly DateOnly CoverEnd = new(2027, 2, 28);

    private readonly InMemoryClaimRepository _claims = new();
    private readonly InMemoryCoverRepository _covers = new();
    private readonly RecordingAuditer _auditer = new();
    private readonly ClaimService _service;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ClaimServiceTests()
    {
        _covers.AddAsync(new Cover
        {
            Id = CoverId,
            StartDate = CoverStart,
            EndDate = CoverEnd,
            Type = CoverType.Yacht,
            Premium = 38_500m
        });
        _service = new ClaimService(_claims, _covers, _auditer, new CreateClaimRequestValidator());
    }

    private static CreateClaimRequest Request(DateOnly created, string coverId = CoverId) =>
        new(coverId, created, "Hull damage", ClaimType.Collision, 5_000m);

    [Fact]
    public async Task A_valid_claim_is_saved_and_audited()
    {
        var request = Request(new DateOnly(2027, 2, 14));

        var result = await _service.CreateAsync(request, CancellationToken);

        var created = Assert.IsType<CreateClaimResult.Created>(result);
        var stored = Assert.Single(_claims.Stored);
        Assert.Equal(
            new ClaimResponse(stored.Id, request.CoverId, request.Created, request.Name, request.Type, request.DamageCost),
            created.Claim);
        Assert.Equal([new Audit("Claim", stored.Id, "POST")], _auditer.Audits);
    }

    [Fact]
    public async Task A_claim_on_the_first_or_last_day_of_the_cover_is_accepted()
    {
        Assert.IsType<CreateClaimResult.Created>(await _service.CreateAsync(Request(CoverStart), CancellationToken));
        Assert.IsType<CreateClaimResult.Created>(await _service.CreateAsync(Request(CoverEnd), CancellationToken));
    }

    [Fact]
    public async Task A_claim_the_day_before_or_after_the_cover_is_rejected()
    {
        foreach (var created in new[] { CoverStart.AddDays(-1), CoverEnd.AddDays(1) })
        {
            var invalid = Assert.IsType<CreateClaimResult.Invalid>(await _service.CreateAsync(Request(created), CancellationToken));
            Assert.Contains(nameof(CreateClaimRequest.Created), invalid.Errors.Keys);
        }

        Assert.Empty(_claims.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task A_claim_for_an_unknown_cover_is_rejected()
    {
        var result = await _service.CreateAsync(Request(new DateOnly(2027, 2, 14), coverId: "unknown"), CancellationToken);

        var invalid = Assert.IsType<CreateClaimResult.Invalid>(result);
        Assert.Contains(nameof(CreateClaimRequest.CoverId), invalid.Errors.Keys);
        Assert.Empty(_claims.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task A_claim_that_fails_validation_is_not_saved()
    {
        var result = await _service.CreateAsync(Request(new DateOnly(2027, 2, 14)) with { DamageCost = 0 }, CancellationToken);

        var invalid = Assert.IsType<CreateClaimResult.Invalid>(result);
        Assert.Contains(nameof(CreateClaimRequest.DamageCost), invalid.Errors.Keys);
        Assert.Empty(_claims.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task Deleting_a_claim_removes_it_and_audits()
    {
        var created = Assert.IsType<CreateClaimResult.Created>(
            await _service.CreateAsync(Request(new DateOnly(2027, 2, 14)), CancellationToken));
        _auditer.Audits.Clear();

        Assert.True(await _service.DeleteAsync(created.Claim.Id, CancellationToken));
        Assert.Empty(_claims.Stored);
        Assert.Equal([new Audit("Claim", created.Claim.Id, "DELETE")], _auditer.Audits);
    }

    [Fact]
    public async Task Deleting_an_unknown_claim_returns_false_and_is_not_audited()
    {
        Assert.False(await _service.DeleteAsync("unknown", CancellationToken));
        Assert.Empty(_auditer.Audits);
    }
}
