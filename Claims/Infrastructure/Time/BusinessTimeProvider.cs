namespace Claims.Infrastructure.Time;

/// <summary>
/// The system clock, with the business time zone as its local time zone, so <see cref="TimeProvider.GetLocalNow"/>
/// gives the insurer's "now" wherever the server runs.
/// </summary>
public sealed class BusinessTimeProvider(TimeZoneInfo businessTimeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => businessTimeZone;
}
