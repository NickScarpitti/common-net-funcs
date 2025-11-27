#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using MemoryPack;
using MessagePack;

using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Requests.Rest.RestHelpersStatic;
namespace BenchmarkSuite;

[MediumRunJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class RestHelpersStaticBenchmarks
{
	private byte[] _jsonData = null!;
	private byte[] _compressedJsonDataGzip = null!;
	private byte[] _compressedJsonDataBrotli = null!;
	private byte[] _textData = null!;
	private byte[] _compressedTextDataGzip = null!;
	private byte[] _largeJsonData = null!;
	private byte[] _compressedLargeJsonDataGzip = null!;
	private TestModel _testModel = null!;
	private Dictionary<string, string> _headers = null!;
	private Dictionary<string, string> _headersWithContentType = null!;

	private const string DummyUrl = "http://";

	[GlobalSetup]
	public void Setup()
	{
		_testModel = new TestModel { Name = "Test User", Value = 12345, Description = "This is a test description with some content to make it more realistic" };
		string jsonString = JsonSerializer.Serialize(_testModel);
		_jsonData = Encoding.UTF8.GetBytes(jsonString);

		// Create a large list for more realistic testing
		List<TestModel> _largeTestModelList = new();
		for (int i = 0; i < 1000; i++)
		{
			_largeTestModelList.Add(new TestModel
			{
				Name = $"Test User {i}",
				Value = i,
				Description = $"This is test description number {i} with some additional content to make it more realistic and test performance with larger payloads"
			});
		}
		string largeJsonString = JsonSerializer.Serialize(_largeTestModelList);
		_largeJsonData = Encoding.UTF8.GetBytes(largeJsonString);

		const string textString = "This is a plain text response that would typically be returned as an error message from an API endpoint.";
		_textData = Encoding.UTF8.GetBytes(textString);

		// Setup headers
		_headers = new Dictionary<string, string>
		{
			{ "Accept", Json },
			{ "User-Agent", "TestAgent/1.0" },
			{ "X-Custom-Header", "CustomValue" }
		};

		_headersWithContentType = new Dictionary<string, string>(_headers)
		{
			{ "Content-Type",Json }
		};

		// Create GZip compressed JSON
		using (MemoryStream compressedStream = new())
		{
			using (GZipStream gzipStream = new(compressedStream, CompressionLevel.Optimal, true))
			{
				gzipStream.Write(_jsonData, 0, _jsonData.Length);
			}
			_compressedJsonDataGzip = compressedStream.ToArray();
		}

		// Create Brotli compressed JSON
		using (MemoryStream compressedStream = new())
		{
			using (BrotliStream brotliStream = new(compressedStream, CompressionLevel.Optimal, true))
			{
				brotliStream.Write(_jsonData, 0, _jsonData.Length);
			}
			_compressedJsonDataBrotli = compressedStream.ToArray();
		}

		// Create GZip compressed text
		using (MemoryStream compressedStream = new())
		{
			using (GZipStream gzipStream = new(compressedStream, CompressionLevel.Optimal, true))
			{
				gzipStream.Write(_textData, 0, _textData.Length);
			}
			_compressedTextDataGzip = compressedStream.ToArray();
		}

		// Create GZip compressed large JSON
		using (MemoryStream compressedStream = new())
		{
			using (GZipStream gzipStream = new(compressedStream, CompressionLevel.Optimal, true))
			{
				gzipStream.Write(_largeJsonData, 0, _largeJsonData.Length);
			}
			_compressedLargeJsonDataGzip = compressedStream.ToArray();
		}
	}

	[Benchmark(Baseline = true)]
	public async Task<TestModel?> ReadResponseStream_Json_NoCompression()
	{
		await using MemoryStream stream = new(_jsonData);
		return await stream.ReadResponseStream<TestModel>(Json, null, false);
	}

	[Benchmark]
	public async Task<TestModel?> ReadResponseStream_Json_GZip()
	{
		await using MemoryStream stream = new(_compressedJsonDataGzip);
		return await stream.ReadResponseStream<TestModel>(Json, "gzip", false);
	}

	[Benchmark]
	public async Task<TestModel?> ReadResponseStream_Json_Brotli()
	{
		await using MemoryStream stream = new(_compressedJsonDataBrotli);
		return await stream.ReadResponseStream<TestModel>(Json, "br", false);
	}

	[Benchmark]
	public async Task<List<TestModel>?> ReadResponseStream_LargeJson_NoCompression()
	{
		await using MemoryStream stream = new(_largeJsonData);
		return await stream.ReadResponseStream<List<TestModel>>(Json, null, false);
	}

	[Benchmark]
	public async Task<List<TestModel>?> ReadResponseStream_LargeJson_GZip()
	{
		await using MemoryStream stream = new(_compressedLargeJsonDataGzip);
		return await stream.ReadResponseStream<List<TestModel>>(Json, "gzip", false);
	}

	[Benchmark]
	public async Task<string?> ReadResponseStream_Text_NoCompression()
	{
		await using MemoryStream stream = new(_textData);
		return await stream.ReadResponseStream<string>("text/plain", null, false);
	}

	[Benchmark]
	public async Task<string?> ReadResponseStream_Text_GZip()
	{
		await using MemoryStream stream = new(_compressedTextDataGzip);
		return await stream.ReadResponseStream<string>("text/plain", "gzip", false);
	}

	[Benchmark]
	public void AddContent_Json()
	{
		HttpRequestMessage request = new(HttpMethod.Post, DummyUrl);
		request.AddContent(HttpMethod.Post, null, _testModel, null);
		request.Dispose();
	}

	[Benchmark]
	public void AddContent_Json_WithHeaders()
	{
		HttpRequestMessage request = new(HttpMethod.Post, DummyUrl);
		request.AddContent(HttpMethod.Post, _headersWithContentType, _testModel, null);
		request.Dispose();
	}

	[Benchmark]
	public void AddContent_MemoryPack()
	{
		HttpRequestMessage request = new(HttpMethod.Post, DummyUrl);
		Dictionary<string, string> headers = new()
		{ { "Content-Type", "application/x-memorypack" } };
		request.AddContent(HttpMethod.Post, headers, _testModel, null);
		request.Dispose();
	}

	[Benchmark]
	public void AddContent_MessagePack()
	{
		HttpRequestMessage request = new(HttpMethod.Post, DummyUrl);
		Dictionary<string, string> headers = new()
		{ { "Content-Type", "application/x-msgpack" } };
		request.AddContent(HttpMethod.Post, headers, _testModel, null);
		request.Dispose();
	}

	[Benchmark]
	public void AttachHeaders_WithBearer()
	{
		HttpRequestMessage request = new(HttpMethod.Get, DummyUrl);
		request.AttachHeaders("test-bearer-token-12345", _headers);
		request.Dispose();
	}

	[Benchmark]
	public void AttachHeaders_WithoutBearer()
	{
		HttpRequestMessage request = new(HttpMethod.Get, DummyUrl);
		request.AttachHeaders(null, _headers);
		request.Dispose();
	}

	[Benchmark]
	public void AttachHeaders_MultipleHeaders()
	{
		HttpRequestMessage request = new(HttpMethod.Get, DummyUrl);
		Dictionary<string, string> manyHeaders = new()
		{
			{ "Accept", Json },
			{ "User-Agent", "TestAgent/1.0" },
			{ "X-Custom-Header-1", "Value1" },
			{ "X-Custom-Header-2", "Value2" },
			{ "X-Custom-Header-3", "Value3" },
			{ "X-Custom-Header-4", "Value4" },
			{ "X-Custom-Header-5", "Value5" }
		};
		request.AttachHeaders("test-token", manyHeaders);
		request.Dispose();
	}
}

[MemoryPackable]
[MessagePackObject(true)]
public partial class TestModel
{
	public string Name { get; set; } = string.Empty;
	public int Value { get; set; }
	public string Description { get; set; } = string.Empty;
}
