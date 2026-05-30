using System.Globalization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace CommonNetFuncs.Web.Api.MsgPack;

/// <summary>
/// MessagePack formatter for <see cref="decimal"/> that accepts both the standard string encoding used by C# (<see cref="DecimalFormatter"/>) and the numeric (integer / float) encodings sent by JavaScript clients — msgpackr always encodes
/// JS <c>number</c> values as ints or floats, never as strings.
/// </summary>
public sealed class FlexibleDecimalFormatter : IMessagePackFormatter<decimal>
{
	public static readonly FlexibleDecimalFormatter Instance = new();
	private FlexibleDecimalFormatter() { }

	public void Serialize(ref MessagePackWriter writer, decimal value, MessagePackSerializerOptions options)
		=> writer.Write(value.ToString(CultureInfo.InvariantCulture));

	public decimal Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
	{
		MessagePackType type = reader.NextMessagePackType;
		if (type == MessagePackType.Float)
		{
			return (decimal)reader.ReadDouble();
		}

		if (type == MessagePackType.Integer)
		{
			return reader.ReadInt64();
		}
		return decimal.Parse(reader.ReadString()!, CultureInfo.InvariantCulture);
	}
}

/// <summary>
/// MessagePack formatter for <see cref="Nullable{T}">decimal?</see> that accepts both the
/// standard string encoding used by C# and numeric encodings sent by JavaScript clients.
/// </summary>
public sealed class FlexibleNullableDecimalFormatter : IMessagePackFormatter<decimal?>
{
	public static readonly FlexibleNullableDecimalFormatter Instance = new();
	private FlexibleNullableDecimalFormatter() { }

	public void Serialize(ref MessagePackWriter writer, decimal? value, MessagePackSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.Write(value.Value.ToString(CultureInfo.InvariantCulture));
	}

	public decimal? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
	{
		if (reader.TryReadNil()) return null;
		MessagePackType type = reader.NextMessagePackType;
		if (type == MessagePackType.Float)
		{
			return (decimal?)reader.ReadDouble();
		}

		if (type == MessagePackType.Integer)
		{
			return reader.ReadInt64();
		}
		return decimal.Parse(reader.ReadString()!, CultureInfo.InvariantCulture);
	}
}

/// <summary>
/// Resolver that intercepts <c>decimal</c> and <c>decimal?</c> requests and returns
/// <see cref="FlexibleDecimalFormatter"/> / <see cref="FlexibleNullableDecimalFormatter"/>
/// instead of the standard <see cref="DecimalFormatter"/> (which rejects numeric msgpack codes).
/// Register before <see cref="StandardResolver"/> in a <see cref="CompositeResolver"/>.
/// </summary>
public sealed class FlexibleDecimalResolver : IFormatterResolver
{
	public static readonly FlexibleDecimalResolver Instance = new();
	private FlexibleDecimalResolver() { }

	public IMessagePackFormatter<T>? GetFormatter<T>()
	{
		if (typeof(T) == typeof(decimal))
		{
			return (IMessagePackFormatter<T>)(object)FlexibleDecimalFormatter.Instance;
		}
		if (typeof(T) == typeof(decimal?))
		{
			return (IMessagePackFormatter<T>)(object)FlexibleNullableDecimalFormatter.Instance;
		}
		return null;
	}
}
