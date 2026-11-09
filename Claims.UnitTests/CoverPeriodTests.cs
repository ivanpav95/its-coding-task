using Claims.Domain;
using Xunit;

namespace Claims.UnitTests;

public class CoverPeriodTests
{
    private static readonly TimeZoneInfo Oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private static readonly DateTimeOffset SixPmInOslo = new(2027, 1, 10, 18, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void A_cover_starting_today_starts_at_the_moment_it_is_bought()
    {
        Assert.Equal(SixPmInOslo, CoverPeriod.StartsAt(new DateOnly(2027, 1, 10), SixPmInOslo, Oslo));
    }

    [Fact]
    public void A_cover_starting_later_starts_at_midnight_in_the_business_time_zone()
    {
        Assert.Equal(new DateTimeOffset(2027, 1, 11, 0, 0, 0, TimeSpan.FromHours(1)),
            CoverPeriod.StartsAt(new DateOnly(2027, 1, 11), SixPmInOslo, Oslo));
        Assert.Equal(new DateTimeOffset(2027, 7, 1, 0, 0, 0, TimeSpan.FromHours(2)),
            CoverPeriod.StartsAt(new DateOnly(2027, 7, 1), SixPmInOslo, Oslo));
    }

    [Fact]
    public void A_cover_ends_at_midnight_after_its_end_date()
    {
        Assert.Equal(new DateTimeOffset(2027, 1, 20, 0, 0, 0, TimeSpan.FromHours(1)),
            CoverPeriod.EndsAt(new DateOnly(2027, 1, 19), Oslo));
    }
}
