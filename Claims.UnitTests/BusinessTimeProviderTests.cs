using Claims.Infrastructure.Time;
using Xunit;

namespace Claims.UnitTests;

public class BusinessTimeProviderTests
{
    [Fact]
    public void Local_now_is_in_the_business_time_zone()
    {
        var oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
        var time = new BusinessTimeProvider(oslo);

        var now = time.GetLocalNow();

        Assert.Same(oslo, time.LocalTimeZone);
        Assert.Equal(oslo.GetUtcOffset(now), now.Offset);
    }
}
