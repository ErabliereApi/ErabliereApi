using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErabliereApi.Mcp.Serialization;

/// <summary>
/// Writes a <see cref="DateTimeOffset"/> as an ISO 8601 instant truncated to the
/// second, keeping the original offset: <c>2026-03-12T06:30:00-04:00</c>.
/// </summary>
/// <remarks>
/// The default converter round-trips the seven fractional digits stored by SQL
/// Server (<c>2026-03-12T06:30:00.1234567-04:00</c>). Sensor readings are never
/// timestamped more precisely than the second, so those eight characters are pure
/// noise: on a hundred point series they alone cost around two hundred tokens.
/// The offset is kept rather than normalized to UTC because a maple grove is read
/// in local time, where the freeze/thaw cycle of a day is what matters.
/// </remarks>
public sealed class Iso8601SecondsConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// Format emitted by this converter, a valid ISO 8601 / RFC 3339 timestamp.
    /// </summary>
    public const string Format = "yyyy-MM-ddTHH:mm:sszzz";

    /// <inheritdoc />
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTimeOffset();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, System.Globalization.CultureInfo.InvariantCulture));
    }
}
