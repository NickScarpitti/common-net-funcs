using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace CommonNetFuncs.Web.Api.MsgPack;

/// <summary>
/// Normalizes DateTime values with DateTimeKind.Unspecified to UTC before JSON serialization
/// so the output always has a trailing "Z", matching the MessagePack formatter behavior.
/// EF Core reads PostgreSQL timestamps as Unspecified even though they are stored as UTC.
/// </summary>
public sealed class DateTimeUtcJsonConverter : JsonConverter<DateTime>
{
	public static readonly DateTimeUtcJsonConverter Instance = new();

	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
	{
		if (value.Kind == DateTimeKind.Unspecified)
		{
			value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
		}
		writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
	}
}


/// <summary>
/// Serializes DateTime as an ISO 8601 string instead of the MessagePack Timestamp
/// extension type (ext -1 / tag 255). This matches how System.Text.Json encodes dates
/// and what the TypeScript interfaces expect (string).
/// </summary>
public sealed class DateTimeAsStringFormatter : IMessagePackFormatter<DateTime>
{
	public static readonly DateTimeAsStringFormatter Instance = new();

	public void Serialize(ref MessagePackWriter writer, DateTime value, MessagePackSerializerOptions options)
	{
		// EF Core returns UTC values from the DB with DateTimeKind.Unspecified, which causes
		// the "O" format to omit the trailing "Z". JavaScript's new Date() then treats the
		// string as local time instead of UTC, breaking toLocaleString(). Normalize to Utc
		// so the "Z" suffix is always emitted.
		if (value.Kind == DateTimeKind.Unspecified)
		{
			value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
		}
		writer.Write(value.ToString("O")); // ISO 8601 round-trip format, e.g. 2026-04-14T12:00:00.0000000Z
	}

	public DateTime Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
	{
		return DateTime.Parse(reader.ReadString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}
}

/// <summary>
/// Serializes TimeSpan as a string in the constant ("c") format, e.g. "01:30:00" for 1 hour 30 minutes.
/// This is more human-readable and interoperable than the default MessagePack array format.
/// </summary>
public sealed class TimeSpanAsStringFormatter : IMessagePackFormatter<TimeSpan>
{
	public static readonly TimeSpanAsStringFormatter Instance = new();

	public void Serialize(ref MessagePackWriter writer, TimeSpan value, MessagePackSerializerOptions options)
	{
		writer.Write(value.ToString("c", CultureInfo.InvariantCulture)); // Constant ("c") format, e.g. "01:30:00" for 1 hour 30 minutes
	}

	public TimeSpan Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
	{
		return TimeSpan.Parse(reader.ReadString()!, CultureInfo.InvariantCulture);
	}
}

/// <summary>
/// Serializes <see cref="DateTimeOffset"/> as an ISO 8601 string in MessagePack.
/// This ensures that <see cref="MessagePackSerializer.ConvertToJson"/> (used in
/// MsgPackRequestMiddleware) produces a JSON string instead of a 2-element array,
/// which System.Text.Json can then deserialize as <see cref="DateTimeOffset"/>.
/// </summary>
public sealed class DateTimeOffsetAsStringFormatter : IMessagePackFormatter<DateTimeOffset>
{
	public static readonly DateTimeOffsetAsStringFormatter Instance = new();

	public void Serialize(ref MessagePackWriter writer, DateTimeOffset value, MessagePackSerializerOptions options)
		=> writer.Write(value.ToString("O", CultureInfo.InvariantCulture));

	public DateTimeOffset Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		=> DateTimeOffset.Parse(reader.ReadString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

public sealed class DateTimeStringResolver : IFormatterResolver
{
	public static readonly DateTimeStringResolver Instance = new();

	public IMessagePackFormatter<T>? GetFormatter<T>()
	{
		return Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.DateTime => (IMessagePackFormatter<T>)(object)DateTimeAsStringFormatter.Instance,
			TypeCode.Object when typeof(T) == typeof(DateTimeOffset) => (IMessagePackFormatter<T>)(object)DateTimeOffsetAsStringFormatter.Instance,
			TypeCode.Object when typeof(T) == typeof(TimeSpan) => (IMessagePackFormatter<T>)(object)TimeSpanAsStringFormatter.Instance,
			_ => null,
		};
	}
}

public static class MsgPackSerializerConfig
{
	public static readonly MessagePackSerializerOptions DateTimesAsStrings =
		MessagePackSerializerOptions.Standard
			.WithSecurity(MessagePackSecurity.UntrustedData)
			.WithResolver(CompositeResolver.Create(DateTimeStringResolver.Instance, StandardResolver.Instance));
}