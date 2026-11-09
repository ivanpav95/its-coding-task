namespace Claims.Domain;

/// <summary>
/// When a cover is in force. Its dates say which days are covered; these moments say from and until when, so a cover
/// bought during its first day starts at the moment of purchase, never earlier.
/// </summary>
public static class CoverPeriod
{
    public static DateTimeOffset StartsAt(DateOnly startDate, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var startOfDay = StartOfDay(startDate, timeZone);
        return startOfDay > now ? startOfDay : now;
    }

    public static DateTimeOffset EndsAt(DateOnly endDate, TimeZoneInfo timeZone) =>
        StartOfDay(endDate.AddDays(1), timeZone);

    private static DateTimeOffset StartOfDay(DateOnly date, TimeZoneInfo timeZone)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, timeZone.GetUtcOffset(midnight));
    }
}
