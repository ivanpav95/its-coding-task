using System.Text.Json;
using Claims.Api;
using Xunit;

namespace Claims.UnitTests;

public class RequireOffsetDateTimeOffsetConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new RequireOffsetDateTimeOffsetConverter() } };

    [Theory]
    [InlineData("\"2026-11-09T10:00:00+01:00\"")]
    [InlineData("\"2026-11-09T09:00:00Z\"")]
    public void A_time_with_an_offset_is_read_as_that_moment(string json)
    {
        var value = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);

        Assert.Equal(new DateTimeOffset(2026, 11, 9, 9, 0, 0, TimeSpan.Zero), value);
    }

    [Theory]
    [InlineData("\"2026-11-09T10:00:00\"")]
    [InlineData("\"2026-11-09\"")]
    public void A_time_without_an_offset_is_rejected(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>(json, Options));
    }
}
