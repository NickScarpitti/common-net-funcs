using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CommonNetFuncs.Compression;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CompressionStreamsBenchmarks
{
	private byte[] smallData = null!;
	private byte[] mediumData = null!;
	private byte[] largeData = null!;
	private byte[] compressedSmallGzip = null!;
	private byte[] compressedMediumGzip = null!;
	private byte[] compressedLargeGzip = null!;
	private Stream seekableStream = null!;
	private Stream nonSeekableStream = null!;

	[GlobalSetup]
	public async Task Setup()
	{
		// Create test data with various patterns for realistic compression
		smallData = GenerateTestData(1024); // 1 KB
		mediumData = GenerateTestData(100 * 1024); // 100 KB
		largeData = GenerateTestData(1024 * 1024); // 1 MB

		// Pre-compress data for decompression benchmarks
		compressedSmallGzip = await smallData.Compress(Streams.ECompressionType.Gzip);
		compressedMediumGzip = await mediumData.Compress(Streams.ECompressionType.Gzip);
		compressedLargeGzip = await largeData.Compress(Streams.ECompressionType.Gzip);

		// Prepare streams for detection benchmarks
		seekableStream = new MemoryStream(compressedSmallGzip);
		nonSeekableStream = new NonSeekableStream(compressedSmallGzip);
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		seekableStream?.Dispose();
		nonSeekableStream?.Dispose();
	}

	private static byte[] GenerateTestData(int size)
	{
		byte[] data = new byte[size];
		Random random = new(42); // Fixed seed for consistency

		// Mix of patterns: some random, some repetitive (more realistic for compression)
		for (int i = 0; i < size; i++)
		{
			if (i % 10 < 7)
			{
				data[i] = (byte)(i % 256); // Repeating pattern (compressible)
			}
			else
			{
				data[i] = (byte)random.Next(256); // Random data
			}
		}
		return data;
	}

	// ===== Compression Benchmarks =====

	[Benchmark]
	public async Task<byte[]> CompressSmall_Gzip()
	{
		return await smallData.Compress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> CompressMedium_Gzip()
	{
		return await mediumData.Compress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> CompressLarge_Gzip()
	{
		return await largeData.Compress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> CompressSmall_Brotli()
	{
		return await smallData.Compress(Streams.ECompressionType.Brotli);
	}

	[Benchmark]
	public async Task<byte[]> CompressMedium_Brotli()
	{
		return await mediumData.Compress(Streams.ECompressionType.Brotli);
	}

	// ===== Decompression Benchmarks =====

	[Benchmark]
	public async Task<byte[]> DecompressSmall_Gzip()
	{
		return await compressedSmallGzip.Decompress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> DecompressMedium_Gzip()
	{
		return await compressedMediumGzip.Decompress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> DecompressLarge_Gzip()
	{
		return await compressedLargeGzip.Decompress(Streams.ECompressionType.Gzip);
	}

	// ===== Stream Compression Benchmarks =====

	[Benchmark]
	public async Task CompressStream_Medium()
	{
		await using MemoryStream inputStream = new(mediumData);
		await using MemoryStream outputStream = new();
		await inputStream.CompressStream(outputStream, Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task DecompressStream_Medium()
	{
		await using MemoryStream inputStream = new(compressedMediumGzip);
		await using MemoryStream outputStream = new();
		await inputStream.DecompressStream(outputStream, Streams.ECompressionType.Gzip);
	}

	// ===== Detection Benchmarks =====

	[Benchmark]
	public async Task<Streams.ECompressionType> DetectCompressionType_Seekable()
	{
		seekableStream.Position = 0;
		return await seekableStream.DetectCompressionType();
	}

	[Benchmark]
	public async Task<Streams.ECompressionType> DetectCompressionType_NonSeekable()
	{
		// Create fresh non-seekable stream for each iteration
		await using NonSeekableStream stream = new(compressedSmallGzip);
		return await stream.DetectCompressionType();
	}

	[Benchmark]
	public async Task<bool> IsDeflateCompressed_Small()
	{
		return await Streams.IsDeflateCompressed(smallData);
	}

	// ===== Helper class for non-seekable streams =====
	private sealed class NonSeekableStream(byte[] data) : Stream
	{
		private readonly MemoryStream innerStream = new(data);

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
			innerStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return innerStream.Read(buffer, offset, count);
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			return innerStream.ReadAsync(buffer, cancellationToken);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				innerStream?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
