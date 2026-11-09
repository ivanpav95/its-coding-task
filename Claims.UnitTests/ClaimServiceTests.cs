using Claims.Application.Auditing;
using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Application.Validation;
using Claims.Domain;
using Claims.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.UnitTests;

public class ClaimServiceTests
{
    private const string CoverId = "cover-1";
    private static readonly TimeSpan Oslo = TimeSpan.FromHours(1);

    // Bought at 18:00 in Oslo on its first day, 1 February; covers up to and including 28 February.
    private static readonly DateTimeOffset StartsAt = new(2027, 2, 1, 18, 0, 0, Oslo);
    private static readonly DateTimeOffset EndsAt = new(2027, 3, 1, 0, 0, 0, Oslo);
    private static readonly DateTimeOffset Now = new(2027, 3, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MidFebruary = new(2027, 2, 14, 10, 0, 0, Oslo);

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
            StartDate = new DateOnly(2027, 2, 1),
            EndDate = new DateOnly(2027, 2, 28),
            StartsAt = StartsAt.UtcDateTime,
            EndsAt = EndsAt.UtcDateTime,
            Type = CoverType.Yacht,
            Premium = 38_500m
        });
        _service = new ClaimService(_claims, _covers, _auditer, new CreateClaimRequestValidator(new FakeTimeProvider(Now)));
    }

    private static CreateClaimRequest Request(DateTimeOffset created, string coverId = CoverId) =>
        new(coverId, created, "Hull damage", ClaimType.Collision, 5_000m);

    [Fact]
    public async Task A_valid_claim_is_saved_and_audited()
    {
        var request = Request(MidFebruary);

        var result = await _service.CreateAsync(request, CancellationToken);

        var created = Assert.IsType<CreateClaimResult.Created>(result);
        var stored = Assert.Single(_claims.Stored);
        Assert.Equal(
            new ClaimResponse(stored.Id, request.CoverId, request.Created, request.Name, request.Type, request.DamageCost),
            created.Claim);
        Assert.Equal([new Audit("Claim", stored.Id, AuditAction.Created)], _auditer.Audits);
    }

    [Fact]
    public async Task A_claim_from_the_moment_the_cover_starts_until_just_before_it_ends_is_accepted()
    {
        Assert.IsType<CreateClaimResult.Created>(await _service.CreateAsync(Request(StartsAt), CancellationToken));
        Assert.IsType<CreateClaimResult.Created>(await _service.CreateAsync(Request(EndsAt.AddMinutes(-1)), CancellationToken));
    }

    [Fact]
    public async Task A_claim_before_the_cover_was_bought_is_rejected_even_on_its_first_day()
    {
        var tenInTheMorning = new DateTimeOffset(2027, 2, 1, 10, 0, 0, Oslo);

        var result = await _service.CreateAsync(Request(tenInTheMorning), CancellationToken);

        var invalid = Assert.IsType<CreateClaimResult.Invalid>(result);
        Assert.Contains(nameof(CreateClaimRequest.Created), invalid.Errors.Keys);
        Assert.Empty(_claims.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task A_claim_just_before_the_cover_starts_or_when_it_ends_is_rejected()
    {
        foreach (var created in new[] { StartsAt.AddMinutes(-1), EndsAt })
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
        var result = await _service.CreateAsync(Request(MidFebruary, coverId: "unknown"), CancellationToken);

        var invalid = Assert.IsType<CreateClaimResult.Invalid>(result);
        Assert.Contains(nameof(CreateClaimRequest.CoverId), invalid.Errors.Keys);
        Assert.Empty(_claims.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task A_claim_that_fails_validation_is_not_saved()
    {
        var result = await _service.CreateAsync(Request(MidFebruary) with { DamageCost = 0 }, CancellationToken);

        var invalid = Assert.IsType<CreateClaimResult.Invalid>(result);
        Assert.Contains(nameof(CreateClaimRequest.DamageCost), invalid.Errors.Keys);
        Assert.Empty(_claims.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task Deleting_a_claim_removes_it_and_audits()
    {
        var created = Assert.IsType<CreateClaimResult.Created>(
            await _service.CreateAsync(Request(MidFebruary), CancellationToken));
        _auditer.Audits.Clear();

        Assert.True(await _service.DeleteAsync(created.Claim.Id, CancellationToken));
        Assert.Empty(_claims.Stored);
        Assert.Equal([new Audit("Claim", created.Claim.Id, AuditAction.Deleted)], _auditer.Audits);
    }

    [Fact]
    public async Task Deleting_an_unknown_claim_returns_false_and_is_not_audited()
    {
        Assert.False(await _service.DeleteAsync("unknown", CancellationToken));
        Assert.Empty(_auditer.Audits);
    }
}
