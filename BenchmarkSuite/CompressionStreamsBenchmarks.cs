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
	private byte[] _smallData = null!;
	private byte[] _mediumData = null!;
	private byte[] _largeData = null!;
	private byte[] _compressedSmallGzip = null!;
	private byte[] _compressedMediumGzip = null!;
	private byte[] _compressedLargeGzip = null!;
	private Stream _seekableStream = null!;
	private Stream _nonSeekableStream = null!;

	[GlobalSetup]
	public async Task Setup()
	{
		// Create test data with various patterns for realistic compression
		_smallData = GenerateTestData(1024); // 1 KB
		_mediumData = GenerateTestData(100 * 1024); // 100 KB
		_largeData = GenerateTestData(1024 * 1024); // 1 MB

		// Pre-compress data for decompression benchmarks
		_compressedSmallGzip = await _smallData.Compress(Streams.ECompressionType.Gzip);
		_compressedMediumGzip = await _mediumData.Compress(Streams.ECompressionType.Gzip);
		_compressedLargeGzip = await _largeData.Compress(Streams.ECompressionType.Gzip);

		// Prepare streams for detection benchmarks
		_seekableStream = new MemoryStream(_compressedSmallGzip);
		_nonSeekableStream = new NonSeekableStream(_compressedSmallGzip);
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		_seekableStream?.Dispose();
		_nonSeekableStream?.Dispose();
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
		return await _smallData.Compress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> CompressMedium_Gzip()
	{
		return await _mediumData.Compress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> CompressLarge_Gzip()
	{
		return await _largeData.Compress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> CompressSmall_Brotli()
	{
		return await _smallData.Compress(Streams.ECompressionType.Brotli);
	}

	[Benchmark]
	public async Task<byte[]> CompressMedium_Brotli()
	{
		return await _mediumData.Compress(Streams.ECompressionType.Brotli);
	}

	// ===== Decompression Benchmarks =====

	[Benchmark]
	public async Task<byte[]> DecompressSmall_Gzip()
	{
		return await _compressedSmallGzip.Decompress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> DecompressMedium_Gzip()
	{
		return await _compressedMediumGzip.Decompress(Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task<byte[]> DecompressLarge_Gzip()
	{
		return await _compressedLargeGzip.Decompress(Streams.ECompressionType.Gzip);
	}

	// ===== Stream Compression Benchmarks =====

	[Benchmark]
	public async Task CompressStream_Medium()
	{
		using MemoryStream inputStream = new(_mediumData);
		using MemoryStream outputStream = new();
		await inputStream.CompressStream(outputStream, Streams.ECompressionType.Gzip);
	}

	[Benchmark]
	public async Task DecompressStream_Medium()
	{
		using MemoryStream inputStream = new(_compressedMediumGzip);
		using MemoryStream outputStream = new();
		await inputStream.DecompressStream(outputStream, Streams.ECompressionType.Gzip);
	}

	// ===== Detection Benchmarks =====

	[Benchmark]
	public async Task<Streams.ECompressionType> DetectCompressionType_Seekable()
	{
		_seekableStream.Position = 0;
		return await Streams.DetectCompressionType(_seekableStream);
	}

	[Benchmark]
	public async Task<Streams.ECompressionType> DetectCompressionType_NonSeekable()
	{
		// Create fresh non-seekable stream for each iteration
		using NonSeekableStream stream = new(_compressedSmallGzip);
		return await Streams.DetectCompressionType(stream);
	}

	[Benchmark]
	public async Task<bool> IsDeflateCompressed_Small()
	{
		return await Streams.IsDeflateCompressed(_smallData);
	}

	// ===== Helper class for non-seekable streams =====
	private sealed class NonSeekableStream(byte[] data) : Stream
	{
		private readonly MemoryStream _innerStream = new(data);

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush() => _innerStream.Flush();
		public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			=> _innerStream.ReadAsync(buffer, cancellationToken);
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_innerStream?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
