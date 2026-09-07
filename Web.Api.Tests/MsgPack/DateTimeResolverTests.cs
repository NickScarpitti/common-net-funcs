using System.Text.Json;
using CommonNetFuncs.Web.Api.MsgPack;
using MessagePack;
using MessagePack.Formatters;
using static Xunit.TestContext;

namespace Web.Api.Tests.MsgPack;

public sealed class DateTimeUtcJsonConverterTests
{
	private static readonly JsonSerializerOptions Options = new() { Converters = { DateTimeUtcJsonConverter.Instance } };

	// -----------------------------------------------------------------------
	// Write
	// -----------------------------------------------------------------------

	[Fact]
	public void Write_UtcDateTime_EmitsZSuffix()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldContain("Z");
	}

	[Fact]
	public void Write_LocalDateTime_EmitsOffsetNotZ()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Local);
		string json = JsonSerializer.Serialize(value, Options);
		// Local kind: "O" format emits +HH:mm or -HH:mm, not a bare Z
		json.ShouldNotBe("\"2026-04-14T12:00:00.0000000Z\"");
	}

	[Fact]
	public void Write_UnspecifiedDateTime_EmitsZSuffix()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Unspecified);
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldContain("Z");
	}

	[Fact]
	public void Write_UnspecifiedDateTime_TreatedAsUtc()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Unspecified);
		string json = JsonSerializer.Serialize(value, Options);
		DateTime roundTripped = JsonSerializer.Deserialize<DateTime>(json, Options);
		roundTripped.Kind.ShouldBe(DateTimeKind.Utc);
		roundTripped.ShouldBe(DateTime.SpecifyKind(value, DateTimeKind.Utc));
	}

	// -----------------------------------------------------------------------
	// Read
	// -----------------------------------------------------------------------

	[Fact]
	public void Read_IsoStringWithZ_ReturnsUtcKind()
	{
		const string json = "\"2026-04-14T12:00:00.0000000Z\"";
		DateTime result = JsonSerializer.Deserialize<DateTime>(json, Options);
		result.Kind.ShouldBe(DateTimeKind.Utc);
		result.Year.ShouldBe(2026);
		result.Month.ShouldBe(4);
		result.Day.ShouldBe(14);
	}

	[Fact]
	public void Read_IsoStringWithoutOffset_ReturnsUnspecifiedKind()
	{
		const string json = "\"2026-04-14T12:00:00.0000000\"";
		DateTime result = JsonSerializer.Deserialize<DateTime>(json, Options);
		result.Kind.ShouldBe(DateTimeKind.Unspecified);
	}

	[Fact]
	public void RoundTrip_UtcDateTime_PreservesValue()
	{
		DateTime original = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);
		string json = JsonSerializer.Serialize(original, Options);
		DateTime result = JsonSerializer.Deserialize<DateTime>(json, Options);
		result.ShouldBe(original);
		result.Kind.ShouldBe(DateTimeKind.Utc);
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		DateTimeUtcJsonConverter.Instance.ShouldBeSameAs(DateTimeUtcJsonConverter.Instance);
	}
}

public sealed class DateTimeAsStringFormatterTests
{
	private static readonly MessagePackSerializerOptions Options = MsgPackSerializerConfig.DateTimesAsStrings;

	// -----------------------------------------------------------------------
	// Serialize
	// -----------------------------------------------------------------------

	[Fact]
	public void Serialize_UtcDateTime_EmitsZSuffix()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);
		byte[] bytes = MessagePackSerializer.Serialize(value, Options, Current.CancellationToken);
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		raw.ShouldEndWith("Z");
	}

	[Fact]
	public void Serialize_UnspecifiedDateTime_EmitsZSuffix()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Unspecified);
		byte[] bytes = MessagePackSerializer.Serialize(value, Options, Current.CancellationToken);
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		raw.ShouldEndWith("Z");
	}

	[Fact]
	public void Serialize_LocalDateTime_DoesNotEndWithZ()
	{
		// Local kind emits +HH:mm or -HH:mm
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Local);
		byte[] bytes = MessagePackSerializer.Serialize(value, Options, Current.CancellationToken);
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		// Local offset may be +00:00 which ends with "0", or -05:00 etc. — just verify it's ISO 8601
		raw.ShouldContain("T");
	}

	// -----------------------------------------------------------------------
	// Deserialize
	// -----------------------------------------------------------------------

	[Fact]
	public void Deserialize_IsoStringWithZ_ReturnsUtcKind()
	{
		DateTime original = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);
		byte[] bytes = MessagePackSerializer.Serialize(original, Options, Current.CancellationToken);
		DateTime result = MessagePackSerializer.Deserialize<DateTime>(bytes, Options, Current.CancellationToken);
		result.Kind.ShouldBe(DateTimeKind.Utc);
		result.ShouldBe(original);
	}

	[Fact]
	public void RoundTrip_UnspecifiedDateTime_PreservesValue()
	{
		DateTime original = new(2026, 4, 14, 8, 30, 0, DateTimeKind.Unspecified);
		byte[] bytes = MessagePackSerializer.Serialize(original, Options, Current.CancellationToken);
		DateTime result = MessagePackSerializer.Deserialize<DateTime>(bytes, Options, Current.CancellationToken);
		result.ShouldBe(DateTime.SpecifyKind(original, DateTimeKind.Utc));
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		DateTimeAsStringFormatter.Instance.ShouldBeSameAs(DateTimeAsStringFormatter.Instance);
	}
}

public sealed class TimeSpanAsStringFormatterTests
{
	private static readonly MessagePackSerializerOptions Options = MsgPackSerializerConfig.DateTimesAsStrings;

	[Theory]
	[InlineData(1, 30, 0, "01:30:00")]
	[InlineData(0, 0, 0, "00:00:00")]
	[InlineData(23, 59, 59, "23:59:59")]
	public void Serialize_EmitsConstantFormat(int hours, int minutes, int seconds, string expected)
	{
		TimeSpan value = new(hours, minutes, seconds);
		byte[] bytes = MessagePackSerializer.Serialize(value, Options, Current.CancellationToken);
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		raw.ShouldBe(expected);
	}

	[Fact]
	public void RoundTrip_PreservesValue()
	{
		TimeSpan original = new(2, 45, 10);
		byte[] bytes = MessagePackSerializer.Serialize(original, Options, Current.CancellationToken);
		TimeSpan result = MessagePackSerializer.Deserialize<TimeSpan>(bytes, Options, Current.CancellationToken);
		result.ShouldBe(original);
	}

	[Fact]
	public void Serialize_NegativeTimeSpan_EmitsCorrectFormat()
	{
		TimeSpan value = new(-1, 0, 0);
		byte[] bytes = MessagePackSerializer.Serialize(value, Options, Current.CancellationToken);
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		raw.ShouldBe("-01:00:00");
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		TimeSpanAsStringFormatter.Instance.ShouldBeSameAs(TimeSpanAsStringFormatter.Instance);
	}
}

public sealed class DateTimeOffsetAsStringFormatterTests
{
	private static readonly MessagePackSerializerOptions Options = MsgPackSerializerConfig.DateTimesAsStrings;

	[Fact]
	public void Serialize_EmitsIso8601String()
	{
		DateTimeOffset value = new(2026, 4, 14, 12, 0, 0, TimeSpan.Zero);
		byte[] bytes = MessagePackSerializer.Serialize(value, Options, Current.CancellationToken);
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		raw.ShouldContain("2026-04-14T12:00:00");
		raw.ShouldContain("+00:00");
	}

	[Fact]
	public void RoundTrip_PreservesValue()
	{
		DateTimeOffset original = new(2026, 4, 14, 12, 0, 0, TimeSpan.FromHours(5));
		byte[] bytes = MessagePackSerializer.Serialize(original, Options, Current.CancellationToken);
		DateTimeOffset result = MessagePackSerializer.Deserialize<DateTimeOffset>(bytes, Options, Current.CancellationToken);
		result.ShouldBe(original);
	}

	[Fact]
	public void RoundTrip_NegativeOffset_PreservesValue()
	{
		DateTimeOffset original = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
		byte[] bytes = MessagePackSerializer.Serialize(original, Options, Current.CancellationToken);
		DateTimeOffset result = MessagePackSerializer.Deserialize<DateTimeOffset>(bytes, Options, Current.CancellationToken);
		result.ShouldBe(original);
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		DateTimeOffsetAsStringFormatter.Instance.ShouldBeSameAs(DateTimeOffsetAsStringFormatter.Instance);
	}
}

public sealed class DateTimeStringResolverTests
{
	[Fact]
	public void GetFormatter_DateTime_ReturnsDateTimeAsStringFormatter()
	{
		IMessagePackFormatter<DateTime>? formatter = DateTimeStringResolver.Instance.GetFormatter<DateTime>();
		formatter.ShouldBeOfType<DateTimeAsStringFormatter>();
	}

	[Fact]
	public void GetFormatter_DateTimeOffset_ReturnsDateTimeOffsetAsStringFormatter()
	{
		IMessagePackFormatter<DateTimeOffset>? formatter = DateTimeStringResolver.Instance.GetFormatter<DateTimeOffset>();
		formatter.ShouldBeOfType<DateTimeOffsetAsStringFormatter>();
	}

	[Fact]
	public void GetFormatter_TimeSpan_ReturnsTimeSpanAsStringFormatter()
	{
		IMessagePackFormatter<TimeSpan>? formatter = DateTimeStringResolver.Instance.GetFormatter<TimeSpan>();
		formatter.ShouldBeOfType<TimeSpanAsStringFormatter>();
	}

	[Fact]
	public void GetFormatter_UnknownType_ReturnsNull()
	{
		IMessagePackFormatter<int>? formatter = DateTimeStringResolver.Instance.GetFormatter<int>();
		formatter.ShouldBeNull();
	}

	[Fact]
	public void GetFormatter_String_ReturnsNull()
	{
		IMessagePackFormatter<string>? formatter = DateTimeStringResolver.Instance.GetFormatter<string>();
		formatter.ShouldBeNull();
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		DateTimeStringResolver.Instance.ShouldBeSameAs(DateTimeStringResolver.Instance);
	}
}

public sealed class MsgPackSerializerConfigTests
{
	[Fact]
	public void DateTimesAsStrings_IsNotNull()
	{
		MsgPackSerializerConfig.DateTimesAsStrings.ShouldNotBeNull();
	}

	[Fact]
	public void DateTimesAsStrings_SerializesDateTimeAsString()
	{
		DateTime value = new(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);
		byte[] bytes = MessagePackSerializer.Serialize(value, MsgPackSerializerConfig.DateTimesAsStrings, Current.CancellationToken);
		// Should be a msgpack str type, deserializable as string
		string raw = MessagePackSerializer.Deserialize<string>(bytes, MessagePackSerializerOptions.Standard, Current.CancellationToken);
		raw.ShouldContain("2026-04-14");
	}

	[Fact]
	public void GetJsonSerializerOptionsWithDateTimeConverters_IncludesDateTimeConverter()
	{
		var options = MsgPackSerializerConfig.GetJsonSerializerOptionsWithDateTimeConverters();
		options.ShouldNotBeNull();
		options.Converters.ShouldContain(c => c is DateTimeUtcJsonConverter);
	}

	[Fact]
	public void GetJsonSerializerOptionsWithDateTimeConverters_IncludesTimeSpanConverter()
	{
		var options = MsgPackSerializerConfig.GetJsonSerializerOptionsWithDateTimeConverters();
		options.Converters.ShouldContain(c => c is TimeSpanJsonConverter);
	}

	[Fact]
	public void GetJsonSerializerOptionsWithDateTimeConverters_IncludesDateTimeOffsetConverter()
	{
		var options = MsgPackSerializerConfig.GetJsonSerializerOptionsWithDateTimeConverters();
		options.Converters.ShouldContain(c => c is DateTimeOffsetJsonConverter);
	}

	[Fact]
	public void GetJsonSerializerOptionsWithDateTimeConverters_CanRoundTripAllTypes()
	{
		var options = MsgPackSerializerConfig.GetJsonSerializerOptionsWithDateTimeConverters();

		// DateTime
		var dt = new DateTime(2026, 4, 14, 12, 0, 0, DateTimeKind.Utc);
		string dtJson = JsonSerializer.Serialize(dt, options);
		var dtResult = JsonSerializer.Deserialize<DateTime>(dtJson, options);
		dtResult.ShouldBe(dt);

		// TimeSpan
		var ts = new TimeSpan(2, 30, 45);
		string tsJson = JsonSerializer.Serialize(ts, options);
		var tsResult = JsonSerializer.Deserialize<TimeSpan>(tsJson, options);
		tsResult.ShouldBe(ts);

		// DateTimeOffset
		var dto = new DateTimeOffset(2026, 4, 14, 12, 0, 0, TimeSpan.FromHours(5));
		string dtoJson = JsonSerializer.Serialize(dto, options);
		var dtoResult = JsonSerializer.Deserialize<DateTimeOffset>(dtoJson, options);
		dtoResult.ShouldBe(dto);
	}
}

public sealed class TimeSpanJsonConverterTests
{
	private static readonly JsonSerializerOptions Options = new() { Converters = { TimeSpanJsonConverter.Instance } };

	// -----------------------------------------------------------------------
	// Write
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData(1, 30, 0, "\"01:30:00\"")]
	[InlineData(0, 0, 0, "\"00:00:00\"")]
	[InlineData(23, 59, 59, "\"23:59:59\"")]
	public void Write_EmitsConstantFormat(int hours, int minutes, int seconds, string expected)
	{
		TimeSpan value = new(hours, minutes, seconds);
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldBe(expected);
	}

	[Fact]
	public void Write_NegativeTimeSpan_EmitsNegativeFormat()
	{
		TimeSpan value = new(-1, 0, 0);
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldBe("\"-01:00:00\"");
	}

	[Fact]
	public void Write_ZeroTimeSpan_EmitsZeros()
	{
		TimeSpan value = TimeSpan.Zero;
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldBe("\"00:00:00\"");
	}

	// -----------------------------------------------------------------------
	// Read
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("\"01:30:00\"", 1, 30, 0)]
	[InlineData("\"00:00:00\"", 0, 0, 0)]
	[InlineData("\"23:59:59\"", 23, 59, 59)]
	public void Read_ConstantFormatString_ReturnsCorrectTimeSpan(string json, int expectedHours, int expectedMinutes, int expectedSeconds)
	{
		TimeSpan result = JsonSerializer.Deserialize<TimeSpan>(json, Options);
		result.Hours.ShouldBe(expectedHours);
		result.Minutes.ShouldBe(expectedMinutes);
		result.Seconds.ShouldBe(expectedSeconds);
	}

	[Fact]
	public void Read_NegativeFormat_ReturnsNegativeTimeSpan()
	{
		const string json = "\"-01:00:00\"";
		TimeSpan result = JsonSerializer.Deserialize<TimeSpan>(json, Options);
		result.ShouldBe(new TimeSpan(-1, 0, 0));
	}

	// -----------------------------------------------------------------------
	// RoundTrip
	// -----------------------------------------------------------------------

	[Fact]
	public void RoundTrip_PreservesValue()
	{
		TimeSpan original = new(2, 45, 10);
		string json = JsonSerializer.Serialize(original, Options);
		TimeSpan result = JsonSerializer.Deserialize<TimeSpan>(json, Options);
		result.ShouldBe(original);
	}

	[Fact]
	public void RoundTrip_NegativeTimeSpan_PreservesValue()
	{
		TimeSpan original = new(-5, -30, -15);
		string json = JsonSerializer.Serialize(original, Options);
		TimeSpan result = JsonSerializer.Deserialize<TimeSpan>(json, Options);
		result.ShouldBe(original);
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		TimeSpanJsonConverter.Instance.ShouldBeSameAs(TimeSpanJsonConverter.Instance);
	}
}

public sealed class DateTimeOffsetJsonConverterTests
{
	private static readonly JsonSerializerOptions Options = new() { Converters = { DateTimeOffsetJsonConverter.Instance } };

	// -----------------------------------------------------------------------
	// Write
	// -----------------------------------------------------------------------

	[Fact]
	public void Write_ZeroOffset_EmitsIso8601WithZeroOffset()
	{
		DateTimeOffset value = new(2026, 4, 14, 12, 0, 0, TimeSpan.Zero);
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldContain("2026-04-14");
		json.ShouldContain("12:00:00");
		json.ShouldContain("00:00"); // Offset portion
	}

	[Fact]
	public void Write_PositiveOffset_EmitsPositiveOffset()
	{
		DateTimeOffset value = new(2026, 4, 14, 12, 0, 0, TimeSpan.FromHours(5));
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldContain("2026-04-14");
		json.ShouldContain("12:00:00");
		json.ShouldContain("05:00"); // Offset portion
	}

	[Fact]
	public void Write_NegativeOffset_EmitsNegativeOffset()
	{
		DateTimeOffset value = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
		string json = JsonSerializer.Serialize(value, Options);
		json.ShouldContain("2026-01-01T00:00:00");
		json.ShouldContain("-08:00");
	}

	// -----------------------------------------------------------------------
	// Read
	// -----------------------------------------------------------------------

	[Fact]
	public void Read_Iso8601WithOffset_ReturnsCorrectDateTimeOffset()
	{
		const string json = "\"2026-04-14T12:00:00+05:00\"";
		DateTimeOffset result = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);
		result.Year.ShouldBe(2026);
		result.Month.ShouldBe(4);
		result.Day.ShouldBe(14);
		result.Hour.ShouldBe(12);
		result.Offset.ShouldBe(TimeSpan.FromHours(5));
	}

	[Fact]
	public void Read_Iso8601WithZeroOffset_ReturnsCorrectDateTimeOffset()
	{
		const string json = "\"2026-04-14T12:00:00+00:00\"";
		DateTimeOffset result = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);
		result.Offset.ShouldBe(TimeSpan.Zero);
	}

	[Fact]
	public void Read_Iso8601WithZSuffix_ReturnsUtcOffset()
	{
		const string json = "\"2026-04-14T12:00:00Z\"";
		DateTimeOffset result = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);
		result.Offset.ShouldBe(TimeSpan.Zero);
	}

	// -----------------------------------------------------------------------
	// RoundTrip
	// -----------------------------------------------------------------------

	[Fact]
	public void RoundTrip_PositiveOffset_PreservesValue()
	{
		DateTimeOffset original = new(2026, 4, 14, 12, 0, 0, TimeSpan.FromHours(5));
		string json = JsonSerializer.Serialize(original, Options);
		DateTimeOffset result = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);
		result.ShouldBe(original);
	}

	[Fact]
	public void RoundTrip_NegativeOffset_PreservesValue()
	{
		DateTimeOffset original = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
		string json = JsonSerializer.Serialize(original, Options);
		DateTimeOffset result = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);
		result.ShouldBe(original);
	}

	[Fact]
	public void RoundTrip_ZeroOffset_PreservesValue()
	{
		DateTimeOffset original = new(2026, 4, 14, 12, 0, 0, TimeSpan.Zero);
		string json = JsonSerializer.Serialize(original, Options);
		DateTimeOffset result = JsonSerializer.Deserialize<DateTimeOffset>(json, Options);
		result.ShouldBe(original);
	}

	[Fact]
	public void Instance_IsSingleton()
	{
		DateTimeOffsetJsonConverter.Instance.ShouldBeSameAs(DateTimeOffsetJsonConverter.Instance);
	}
}
