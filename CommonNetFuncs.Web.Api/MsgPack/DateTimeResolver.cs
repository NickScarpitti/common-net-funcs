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
/// Serializes TimeSpan as a string in the constant ("c") format, e.g. "01:30:00" for 1 hour 30 minutes.
/// This matches the MessagePack formatter behavior and is more human-readable.
/// </summary>
public sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
	public static readonly TimeSpanJsonConverter Instance = new();

	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> TimeSpan.Parse(reader.GetString()!, CultureInfo.InvariantCulture);

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
}

/// <summary>
/// Serializes DateTimeOffset as an ISO 8601 string in JSON.
/// This matches the MessagePack formatter behavior and ensures consistent round-trip serialization.
/// </summary>
public sealed class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
	public static readonly DateTimeOffsetJsonConverter Instance = new();

	public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

	public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
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

	/// <summary>
	/// Provides a pre-configured <see cref="JsonSerializerOptions"/> with converters for DateTime, DateTimeOffset,
	/// and TimeSpan that match the behavior of <see cref="DateTimesAsStrings"/>.
	///
	/// Use this when deserializing JSON that was created from MessagePack using <see cref="DateTimesAsStrings"/>,
	/// or when you need JSON serialization behavior consistent with the MessagePack formatters.
	/// </summary>
	public static JsonSerializerOptions GetJsonSerializerOptionsWithDateTimeConverters()
	{
		return new JsonSerializerOptions
		{
			Converters =
			{
				DateTimeUtcJsonConverter.Instance,
				TimeSpanJsonConverter.Instance,
				DateTimeOffsetJsonConverter.Instance
			}
		};
	}
}