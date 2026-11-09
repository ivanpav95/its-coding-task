using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claims.Api;

/// <summary>
/// Rejects a time without a UTC offset ("2026-11-09T10:00:00"). By default it would be read in the server's
/// time zone, so the same request would mean a different moment on a different server.
/// </summary>
public sealed class RequireOffsetDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.GetDateTime().Kind == DateTimeKind.Unspecified)
        {
            throw new JsonException("The time must include a UTC offset, e.g. 2026-11-09T10:00:00+01:00 or 2026-11-09T09:00:00Z.");
        }

        return reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
