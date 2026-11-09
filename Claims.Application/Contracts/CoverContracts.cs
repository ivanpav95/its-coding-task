using Claims.Domain;

namespace Claims.Application.Contracts;

public record CreateCoverRequest(DateOnly StartDate, DateOnly EndDate, CoverType Type);

public record CoverResponse(
    string Id,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    CoverType Type,
    decimal Premium)
{
    public static CoverResponse From(Cover cover) =>
        new(cover.Id, cover.StartDate, cover.EndDate,
            new DateTimeOffset(cover.StartsAt, TimeSpan.Zero), new DateTimeOffset(cover.EndsAt, TimeSpan.Zero),
            cover.Type, cover.Premium);
}
