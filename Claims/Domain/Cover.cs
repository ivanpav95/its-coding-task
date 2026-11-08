namespace Claims.Domain;

public class Cover
{
    public required string Id { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    /// <summary>The moment the cover comes into force, in UTC.</summary>
    public DateTime StartsAt { get; init; }

    /// <summary>The moment the cover ends (exclusive), in UTC: midnight after <see cref="EndDate"/>.</summary>
    public DateTime EndsAt { get; init; }

    public CoverType Type { get; init; }

    public decimal Premium { get; init; }
}

public enum CoverType
{
    Yacht = 0,
    PassengerShip = 1,
    ContainerShip = 2,
    BulkCarrier = 3,
    Tanker = 4
}
