using System.Buffers;
using System.Globalization;
using CommonNetFuncs.Web.Api.MsgPack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Web.Api.Tests.MsgPack;

public sealed class FlexibleDecimalFormatterTests
{
		private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard;

		// -----------------------------------------------------------------------
		// Serialize
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData("0")]
		[InlineData("1.23")]
		[InlineData("-9999999999999999999999999999")]
		[InlineData("79228162514264337593543950335")] // decimal.MaxValue
		public void Serialize_WritesStringRepresentation(string valueStr)
		{
				decimal value = decimal.Parse(valueStr, CultureInfo.InvariantCulture);

				byte[] bytes = Serialize(value);
				string result = MessagePackSerializer.Deserialize<string>(bytes, Options);

				result.ShouldBe(value.ToString(CultureInfo.InvariantCulture));
		}

		// -----------------------------------------------------------------------
		// Deserialize – string encoding (C# round-trip)
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData("0")]
		[InlineData("1.23")]
		[InlineData("-1.23")]
		[InlineData("79228162514264337593543950335")]
		public void Deserialize_FromString_ReturnsCorrectDecimal(string valueStr)
		{
				decimal expected = decimal.Parse(valueStr, CultureInfo.InvariantCulture);
				byte[] bytes = MessagePackSerializer.Serialize(valueStr, Options);

				decimal result = Deserialize(bytes);

				result.ShouldBe(expected);
		}

		// -----------------------------------------------------------------------
		// Deserialize – integer encoding (JS path)
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData(0L)]
		[InlineData(42L)]
		[InlineData(-100L)]
		[InlineData(long.MaxValue)]
		[InlineData(long.MinValue)]
		public void Deserialize_FromInteger_ReturnsCorrectDecimal(long intValue)
		{
				byte[] bytes = MessagePackSerializer.Serialize(intValue, Options);

				decimal result = Deserialize(bytes);

				result.ShouldBe((decimal)intValue);
		}

		// -----------------------------------------------------------------------
		// Deserialize – float encoding (JS path)
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData(0.0)]
		[InlineData(1.5)]
		[InlineData(-3.14)]
		public void Deserialize_FromFloat_ReturnsCorrectDecimal(double floatValue)
		{
				byte[] bytes = MessagePackSerializer.Serialize(floatValue, Options);

				decimal result = Deserialize(bytes);

				result.ShouldBe((decimal)floatValue);
		}

		// -----------------------------------------------------------------------
		// Singleton
		// -----------------------------------------------------------------------

		[Fact]
		public void Instance_IsSingleton()
		{
				FlexibleDecimalFormatter.Instance.ShouldBeSameAs(FlexibleDecimalFormatter.Instance);
		}

		// -----------------------------------------------------------------------
		// Helpers
		// -----------------------------------------------------------------------

		private static byte[] Serialize(decimal value)
		{
				ArrayBufferWriter<byte> buffer = new();
				MessagePackWriter writer = new(buffer);
				FlexibleDecimalFormatter.Instance.Serialize(ref writer, value, Options);
				writer.Flush();
				return buffer.WrittenMemory.ToArray();
		}

		private static decimal Deserialize(byte[] bytes)
		{
				MessagePackReader reader = new(bytes);
				return FlexibleDecimalFormatter.Instance.Deserialize(ref reader, Options);
		}
}

public sealed class FlexibleNullableDecimalFormatterTests
{
		private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard;

		// -----------------------------------------------------------------------
		// Serialize – null
		// -----------------------------------------------------------------------

		[Fact]
		public void Serialize_NullValue_WritesNil()
		{
				byte[] bytes = Serialize(null);
				MessagePackReader reader = new(bytes);
				reader.TryReadNil().ShouldBeTrue();
		}

		// -----------------------------------------------------------------------
		// Serialize – non-null
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData("1.23")]
		[InlineData("0")]
		[InlineData("-99.99")]
		public void Serialize_NonNullValue_WritesStringRepresentation(string valueStr)
		{
				decimal? value = decimal.Parse(valueStr, CultureInfo.InvariantCulture);

				byte[] bytes = Serialize(value);
				string result = MessagePackSerializer.Deserialize<string>(bytes, Options);

				result.ShouldBe(value.Value.ToString(CultureInfo.InvariantCulture));
		}

		// -----------------------------------------------------------------------
		// Deserialize – nil
		// -----------------------------------------------------------------------

		[Fact]
		public void Deserialize_FromNil_ReturnsNull()
		{
				ArrayBufferWriter<byte> buffer = new();
				MessagePackWriter writer = new(buffer);
				writer.WriteNil();
				writer.Flush();

				decimal? result = Deserialize(buffer.WrittenMemory.ToArray());

				result.ShouldBeNull();
		}

		// -----------------------------------------------------------------------
		// Deserialize – string
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData("1.23")]
		[InlineData("0")]
		[InlineData("-5.5")]
		public void Deserialize_FromString_ReturnsCorrectNullableDecimal(string valueStr)
		{
				decimal expected = decimal.Parse(valueStr, CultureInfo.InvariantCulture);
				byte[] bytes = MessagePackSerializer.Serialize(valueStr, Options);

				decimal? result = Deserialize(bytes);

				result.ShouldBe(expected);
		}

		// -----------------------------------------------------------------------
		// Deserialize – integer
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData(0L)]
		[InlineData(7L)]
		[InlineData(-7L)]
		public void Deserialize_FromInteger_ReturnsCorrectNullableDecimal(long intValue)
		{
				byte[] bytes = MessagePackSerializer.Serialize(intValue, Options);

				decimal? result = Deserialize(bytes);

				result.ShouldBe((decimal?)intValue);
		}

		// -----------------------------------------------------------------------
		// Deserialize – float
		// -----------------------------------------------------------------------

		[Theory]
		[InlineData(0.0)]
		[InlineData(2.5)]
		[InlineData(-1.1)]
		public void Deserialize_FromFloat_ReturnsCorrectNullableDecimal(double floatValue)
		{
				byte[] bytes = MessagePackSerializer.Serialize(floatValue, Options);

				decimal? result = Deserialize(bytes);

				result.ShouldBe((decimal?)floatValue);
		}

		// -----------------------------------------------------------------------
		// Singleton
		// -----------------------------------------------------------------------

		[Fact]
		public void Instance_IsSingleton()
		{
				FlexibleNullableDecimalFormatter.Instance.ShouldBeSameAs(FlexibleNullableDecimalFormatter.Instance);
		}

		// -----------------------------------------------------------------------
		// Helpers
		// -----------------------------------------------------------------------

		private static byte[] Serialize(decimal? value)
		{
				ArrayBufferWriter<byte> buffer = new();
				MessagePackWriter writer = new(buffer);
				FlexibleNullableDecimalFormatter.Instance.Serialize(ref writer, value, Options);
				writer.Flush();
				return buffer.WrittenMemory.ToArray();
		}

		private static decimal? Deserialize(byte[] bytes)
		{
				MessagePackReader reader = new(bytes);
				return FlexibleNullableDecimalFormatter.Instance.Deserialize(ref reader, Options);
		}
}

public sealed class FlexibleDecimalResolverTests
{
		// -----------------------------------------------------------------------
		// GetFormatter<T>
		// -----------------------------------------------------------------------

		[Fact]
		public void GetFormatter_ForDecimal_ReturnsFlexibleDecimalFormatter()
		{
				IMessagePackFormatter<decimal>? formatter = FlexibleDecimalResolver.Instance.GetFormatter<decimal>();

				formatter.ShouldNotBeNull();
				((object)formatter).ShouldBeSameAs(FlexibleDecimalFormatter.Instance);
		}

		[Fact]
		public void GetFormatter_ForNullableDecimal_ReturnsFlexibleNullableDecimalFormatter()
		{
				IMessagePackFormatter<decimal?>? formatter = FlexibleDecimalResolver.Instance.GetFormatter<decimal?>();

				formatter.ShouldNotBeNull();
				((object)formatter).ShouldBeSameAs(FlexibleNullableDecimalFormatter.Instance);
		}

		[Fact]
		public void GetFormatter_ForUnrelatedType_ReturnsNull()
		{
				IMessagePackFormatter<string>? formatter = FlexibleDecimalResolver.Instance.GetFormatter<string>();

				formatter.ShouldBeNull();
		}

		[Fact]
		public void GetFormatter_ForInt_ReturnsNull()
		{
				IMessagePackFormatter<int>? formatter = FlexibleDecimalResolver.Instance.GetFormatter<int>();

				formatter.ShouldBeNull();
		}

		// -----------------------------------------------------------------------
		// Singleton
		// -----------------------------------------------------------------------

		[Fact]
		public void Instance_IsSingleton()
		{
				FlexibleDecimalResolver.Instance.ShouldBeSameAs(FlexibleDecimalResolver.Instance);
		}

		// -----------------------------------------------------------------------
		// End-to-end: resolver used inside a CompositeResolver
		// -----------------------------------------------------------------------

		[Fact]
		public void EndToEnd_DecimalRoundTrip_WithCompositeResolver()
		{
				MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
						.WithResolver(CompositeResolver.Create(FlexibleDecimalResolver.Instance, StandardResolver.Instance));

				decimal original = 123.456m;
				byte[] bytes = MessagePackSerializer.Serialize(original, options);
				decimal result = MessagePackSerializer.Deserialize<decimal>(bytes, options);

				result.ShouldBe(original);
		}

		[Fact]
		public void EndToEnd_NullableDecimalRoundTrip_WithCompositeResolver()
		{
				MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
						.WithResolver(CompositeResolver.Create(FlexibleDecimalResolver.Instance, StandardResolver.Instance));

				decimal? original = 99.9m;
				byte[] bytes = MessagePackSerializer.Serialize(original, options);
				decimal? result = MessagePackSerializer.Deserialize<decimal?>(bytes, options);

				result.ShouldBe(original);
		}

		[Fact]
		public void EndToEnd_NullableDecimalNull_WithCompositeResolver()
		{
				MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
						.WithResolver(CompositeResolver.Create(FlexibleDecimalResolver.Instance, StandardResolver.Instance));

				decimal? original = null;
				byte[] bytes = MessagePackSerializer.Serialize(original, options);
				decimal? result = MessagePackSerializer.Deserialize<decimal?>(bytes, options);

				result.ShouldBeNull();
		}
}
