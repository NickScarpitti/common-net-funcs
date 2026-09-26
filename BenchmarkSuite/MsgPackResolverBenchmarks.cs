using System;
using BenchmarkDotNet.Attributes;
using MessagePack.Formatters;

namespace BenchmarkSuite;

/// <summary>
/// Benchmarks for MessagePack DateTime resolver implementations.
/// Tests the efficiency of different approaches to GetFormatter&lt;T&gt;.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
public class MsgPackResolverBenchmarks
{
	private CurrentResolver currentResolver = null!;
	private CachedResolver cachedResolver = null!;

	[GlobalSetup]
	public void Setup()
	{
		currentResolver = new CurrentResolver();
		cachedResolver = new CachedResolver();
	}

	#region Current Implementation Tests

	[Benchmark(Baseline = true)]
	public IMessagePackFormatter<DateTime>? Current_DateTime()
	{
		return currentResolver.GetFormatter<DateTime>();
	}

	[Benchmark]
	public IMessagePackFormatter<DateTimeOffset>? Current_DateTimeOffset()
	{
		return currentResolver.GetFormatter<DateTimeOffset>();
	}

	[Benchmark]
	public IMessagePackFormatter<TimeSpan>? Current_TimeSpan()
	{
		return currentResolver.GetFormatter<TimeSpan>();
	}

	[Benchmark]
	public IMessagePackFormatter<DateOnly>? Current_DateOnly()
	{
		return currentResolver.GetFormatter<DateOnly>();
	}

	[Benchmark]
	public IMessagePackFormatter<string>? Current_String()
	{
		return currentResolver.GetFormatter<string>();
	}

	#endregion

	#region Cached Implementation Tests

	[Benchmark]
	public IMessagePackFormatter<DateTime>? Cached_DateTime()
	{
		return cachedResolver.GetFormatter<DateTime>();
	}

	[Benchmark]
	public IMessagePackFormatter<DateTimeOffset>? Cached_DateTimeOffset()
	{
		return cachedResolver.GetFormatter<DateTimeOffset>();
	}

	[Benchmark]
	public IMessagePackFormatter<TimeSpan>? Cached_TimeSpan()
	{
		return cachedResolver.GetFormatter<TimeSpan>();
	}

	[Benchmark]
	public IMessagePackFormatter<DateOnly>? Cached_DateOnly()
	{
		return cachedResolver.GetFormatter<DateOnly>();
	}

	[Benchmark]
	public IMessagePackFormatter<string>? Cached_String()
	{
		return cachedResolver.GetFormatter<string>();
	}

	#endregion
}

/// <summary>
/// Current implementation using Type.GetTypeCode switch.
/// Evaluated on every call.
/// </summary>
public sealed class CurrentResolver
{
	public IMessagePackFormatter<T>? GetFormatter<T>()
	{
		return Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.DateTime => (IMessagePackFormatter<T>)(object)DateTimeAsStringFormatterStub.Instance,
			TypeCode.Object when typeof(T) == typeof(DateTimeOffset) => (IMessagePackFormatter<T>)(object)DateTimeOffsetAsStringFormatterStub.Instance,
			TypeCode.Object when typeof(T) == typeof(TimeSpan) => (IMessagePackFormatter<T>)(object)TimeSpanAsStringFormatterStub.Instance,
			TypeCode.Object when typeof(T) == typeof(DateOnly) => (IMessagePackFormatter<T>)(object)DateOnlyAsStringFormatterStub.Instance,
			_ => null,
		};
	}
}

/// <summary>
/// Optimized implementation using static generic caching.
/// The switch is evaluated only once per type, at class initialization.
/// Subsequent calls just return a cached reference.
/// </summary>
public sealed class CachedResolver
{
	public IMessagePackFormatter<T>? GetFormatter<T>()
	{
		return CachedFormatter<T>.Value;
	}

	// Static generic class pattern: one instance per type, initialized once
	private static class CachedFormatter<T>
	{
		public static readonly IMessagePackFormatter<T>? Value = GetFormatterInternal();

		private static IMessagePackFormatter<T>? GetFormatterInternal()
		{
			return Type.GetTypeCode(typeof(T)) switch
			{
				TypeCode.DateTime => (IMessagePackFormatter<T>)(object)DateTimeAsStringFormatterStub.Instance,
				TypeCode.Object when typeof(T) == typeof(DateTimeOffset) => (IMessagePackFormatter<T>)(object)DateTimeOffsetAsStringFormatterStub.Instance,
				TypeCode.Object when typeof(T) == typeof(TimeSpan) => (IMessagePackFormatter<T>)(object)TimeSpanAsStringFormatterStub.Instance,
				TypeCode.Object when typeof(T) == typeof(DateOnly) => (IMessagePackFormatter<T>)(object)DateOnlyAsStringFormatterStub.Instance,
				_ => null,
			};
		}
	}
}

// Stub formatters for benchmarking purposes (avoid adding reference to actual DateTimeResolver)
public sealed class DateTimeAsStringFormatterStub : IMessagePackFormatter<DateTime>
{
	public static readonly DateTimeAsStringFormatterStub Instance = new();
	public void Serialize(ref MessagePack.MessagePackWriter writer, DateTime value, MessagePack.MessagePackSerializerOptions options) { }
	public DateTime Deserialize(ref MessagePack.MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) => default;
}

public sealed class DateTimeOffsetAsStringFormatterStub : IMessagePackFormatter<DateTimeOffset>
{
	public static readonly DateTimeOffsetAsStringFormatterStub Instance = new();
	public void Serialize(ref MessagePack.MessagePackWriter writer, DateTimeOffset value, MessagePack.MessagePackSerializerOptions options) { }
	public DateTimeOffset Deserialize(ref MessagePack.MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) => default;
}

public sealed class TimeSpanAsStringFormatterStub : IMessagePackFormatter<TimeSpan>
{
	public static readonly TimeSpanAsStringFormatterStub Instance = new();
	public void Serialize(ref MessagePack.MessagePackWriter writer, TimeSpan value, MessagePack.MessagePackSerializerOptions options) { }
	public TimeSpan Deserialize(ref MessagePack.MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) => default;
}

public sealed class DateOnlyAsStringFormatterStub : IMessagePackFormatter<DateOnly>
{
	public static readonly DateOnlyAsStringFormatterStub Instance = new();
	public void Serialize(ref MessagePack.MessagePackWriter writer, DateOnly value, MessagePack.MessagePackSerializerOptions options) { }
	public DateOnly Deserialize(ref MessagePack.MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) => default;
}
