using Claims.Application.Contracts;
using Claims.Application.Services;
using Claims.Application.Validation;
using Claims.Domain;
using Claims.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.UnitTests;

public class CoverServiceTests
{
    private static readonly DateOnly Today = new(2027, 1, 10);

    private static readonly DateTimeOffset Now = new(2027, 1, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryCoverRepository _covers = new();
    private readonly InMemoryClaimRepository _claims = new();
    private readonly RecordingAuditer _auditer = new();
    private readonly CoverService _service;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public CoverServiceTests()
    {
        var time = new FakeTimeProvider(Now);
        time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"));
        _service = new CoverService(_covers, _claims, _auditer, new CreateCoverRequestValidator(time), time);
    }

    [Fact]
    public async Task A_valid_cover_is_saved_with_its_premium_and_audited()
    {
        var result = await _service.CreateAsync(new CreateCoverRequest(Today, Today.AddDays(9), CoverType.Yacht), CancellationToken);

        var created = Assert.IsType<CreateCoverResult.Created>(result);
        var stored = Assert.Single(_covers.Stored);
        Assert.Equal(CoverResponse.From(stored), created.Cover);
        Assert.Equal(13_750m, stored.Premium); // 10 days at the Yacht day rate of 1,375
        Assert.Equal([new Audit("Cover", stored.Id, "POST")], _auditer.Audits);
    }

    [Fact]
    public async Task A_cover_starting_today_is_in_force_from_now_until_midnight_after_its_end_date()
    {
        await _service.CreateAsync(new CreateCoverRequest(Today, Today.AddDays(9), CoverType.Yacht), CancellationToken);

        var stored = Assert.Single(_covers.Stored);
        Assert.Equal(Now.UtcDateTime, stored.StartsAt);
        Assert.Equal(new DateTime(2027, 1, 19, 23, 0, 0, DateTimeKind.Utc), stored.EndsAt); // 20 January 00:00 in Oslo
    }

    [Fact]
    public async Task An_invalid_cover_is_not_saved_or_audited()
    {
        var result = await _service.CreateAsync(new CreateCoverRequest(Today.AddDays(-1), Today, CoverType.Yacht), CancellationToken);

        var invalid = Assert.IsType<CreateCoverResult.Invalid>(result);
        Assert.Contains(nameof(CreateCoverRequest.StartDate), invalid.Errors.Keys);
        Assert.Empty(_covers.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task Deleting_a_cover_removes_it_and_audits()
    {
        var cover = await AddCoverAsync();

        var result = await _service.DeleteAsync(cover.Id, CancellationToken);

        Assert.Equal(DeleteCoverResult.Deleted, result);
        Assert.Empty(_covers.Stored);
        Assert.Equal([new Audit("Cover", cover.Id, "DELETE")], _auditer.Audits);
    }

    [Fact]
    public async Task Deleting_an_unknown_cover_is_not_found_and_not_audited()
    {
        var result = await _service.DeleteAsync("unknown", CancellationToken);

        Assert.Equal(DeleteCoverResult.NotFound, result);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task A_cover_with_claims_is_kept_and_not_audited()
    {
        var cover = await AddCoverAsync();
        await _claims.AddAsync(new Claim
        {
            Id = "claim-1",
            CoverId = cover.Id,
            Created = Now.UtcDateTime,
            Name = "Hull damage",
            Type = ClaimType.Collision,
            DamageCost = 5_000m
        }, CancellationToken);

        var result = await _service.DeleteAsync(cover.Id, CancellationToken);

        Assert.Equal(DeleteCoverResult.HasClaims, result);
        Assert.Single(_covers.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task Computing_a_premium_saves_nothing()
    {
        var result = await _service.ComputePremiumAsync(new CreateCoverRequest(Today, Today.AddDays(9), CoverType.Yacht), CancellationToken);

        Assert.Equal(new ComputePremiumResult.Computed(13_750m), result);
        Assert.Empty(_covers.Stored);
        Assert.Empty(_auditer.Audits);
    }

    [Fact]
    public async Task Computing_a_premium_for_an_invalid_period_is_rejected()
    {
        var result = await _service.ComputePremiumAsync(new CreateCoverRequest(Today, Today.AddDays(-1), CoverType.Yacht), CancellationToken);

        var invalid = Assert.IsType<ComputePremiumResult.Invalid>(result);
        Assert.Contains(nameof(CreateCoverRequest.EndDate), invalid.Errors.Keys);
    }

    private async Task<Cover> AddCoverAsync()
    {
        var cover = new Cover
        {
            Id = "cover-1",
            StartDate = Today,
            EndDate = Today.AddDays(9),
            Type = CoverType.Yacht,
            Premium = 13_750m
        };
        await _covers.AddAsync(cover, CancellationToken);
        return cover;
    }
}
