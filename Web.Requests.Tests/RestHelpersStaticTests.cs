using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest;
using MemoryPack;
using MessagePack;

namespace Web.Requests.Tests;

public sealed class RestHelpersStaticTests
{
	public enum ExceptionType { TaskCanceledExpected, TaskCanceledUnexpected, GeneralException }
	public enum TimeoutValue { Null, Zero }
	public enum CompressionType { GZip, Brotli }
	[Theory]
	[InlineData("GET", null)]
	[InlineData("POST", "body")]
	[InlineData("PUT", "body")]
	public async Task RestRequest_ReturnsExpectedResult(string methodString, string? body)
	{
		HttpMethod method = new(methodString);
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = method,
			BodyObject = body,
			Timeout = 1,
			LogQuery = true,
			LogBody = true
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"result\"", Encoding.UTF8, "application/json")
		};

		string? result = await client.RestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe("result");
	}

	[Theory]
	[InlineData(ExceptionType.TaskCanceledExpected)]
	[InlineData(ExceptionType.TaskCanceledUnexpected)]
	[InlineData(ExceptionType.GeneralException)]
	public async Task RestRequest_HandlesExceptions(ExceptionType exceptionType)
	{
		Exception exception = exceptionType == ExceptionType.GeneralException
			? new InvalidOperationException("fail")
			: new TaskCanceledException("Canceled!");

		FakeHttpMessageHandler handler = new() { ThrowOnSend = exception };
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			ExpectTaskCancellation = exceptionType == ExceptionType.TaskCanceledExpected
		};

		string? result = await client.RestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(TimeoutValue.Null)]
	[InlineData(TimeoutValue.Zero)]
	public async Task RestRequest_UsesDefaultTimeout(TimeoutValue timeoutValue)
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			Timeout = timeoutValue == TimeoutValue.Null ? null : 0
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"result\"", Encoding.UTF8, "application/json")
		};

		string? result = await client.RestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe("result");
	}

	[Fact]
	public async Task RestObjectRequest_ReturnsRestObject()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"foo\"", Encoding.UTF8, "application/json")
		};

		RestObject<string> result = await client.RestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Result.ShouldBe("foo");
		result.Response.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(ExceptionType.TaskCanceledExpected)]
	[InlineData(ExceptionType.TaskCanceledUnexpected)]
	[InlineData(ExceptionType.GeneralException)]
	public async Task RestObjectRequest_HandlesExceptions(ExceptionType exceptionType)
	{
		Exception exception = exceptionType == ExceptionType.GeneralException
			? new InvalidOperationException("fail")
			: new TaskCanceledException("Canceled!");

		FakeHttpMessageHandler handler = new() { ThrowOnSend = exception };
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			ExpectTaskCancellation = exceptionType == ExceptionType.TaskCanceledExpected
		};

		RestObject<string> result = await client.RestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Result.ShouldBeNull();
		if (exceptionType == ExceptionType.TaskCanceledExpected)
		{
			result.Response.ShouldBeNull();
		}
	}

	[Fact]
	public async Task StreamingRestRequest_YieldsResults()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\",\"b\"]", Encoding.UTF8, "application/json")
		};

		List<string?> results = new();
		await foreach (string? item in client.StreamingRestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task StreamingRestRequest_HandlesNullHeaders()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			HttpHeaders = null
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\",\"b\"]", Encoding.UTF8, "application/json")
		};

		List<string?> results = new();
		await foreach (string? item in client.StreamingRestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task StreamingRestRequest_ReplacesAcceptHeader()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			HttpHeaders = new Dictionary<string, string> { { "Accept", "text/xml" } }
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\",\"b\"]", Encoding.UTF8, "application/json")
		};

		List<string?> results = new();
		await foreach (string? item in client.StreamingRestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Theory]
	[InlineData(ExceptionType.TaskCanceledExpected)]
	[InlineData(ExceptionType.TaskCanceledUnexpected)]
	[InlineData(ExceptionType.GeneralException)]
	public async Task StreamingRestRequest_HandlesExceptions(ExceptionType exceptionType)
	{
		Exception exception = exceptionType == ExceptionType.GeneralException
			? new InvalidOperationException("fail")
			: new TaskCanceledException("Canceled!");

		FakeHttpMessageHandler handler = new() { ThrowOnSend = exception };
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			ExpectTaskCancellation = exceptionType == ExceptionType.TaskCanceledExpected
		};

		List<string?> results = new();
		await foreach (string? item in client.StreamingRestRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task StreamingRestObjectRequest_ReturnsStreamingRestObject()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\",\"b\"]", Encoding.UTF8, "application/json")
		};

		StreamingRestObject<string> result = await client.StreamingRestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Response.ShouldNotBeNull();
		result.Result.ShouldNotBeNull();

		List<string?> items = new();
		await foreach (string? item in result.Result!)
		{
			items.Add(item);
		}
		items.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task StreamingRestObjectRequest_HandlesNullHeaders()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			HttpHeaders = null
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\"]", Encoding.UTF8, "application/json")
		};

		StreamingRestObject<string> result = await client.StreamingRestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Response.ShouldNotBeNull();
	}

	[Fact]
	public async Task StreamingRestObjectRequest_KeepsJsonAcceptHeader()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			HttpHeaders = new Dictionary<string, string> { { "Accept", "application/json" } }
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\"]", Encoding.UTF8, "application/json")
		};

		StreamingRestObject<string> result = await client.StreamingRestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Response.ShouldNotBeNull();
	}

	[Fact]
	public async Task StreamingRestObjectRequest_ReplacesNonJsonAcceptHeader()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			HttpHeaders = new Dictionary<string, string> { { "Accept", "text/xml" } }
		};

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\"]", Encoding.UTF8, "application/json")
		};

		StreamingRestObject<string> result = await client.StreamingRestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Response.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(ExceptionType.TaskCanceledExpected)]
	[InlineData(ExceptionType.GeneralException)]
	public async Task StreamingRestObjectRequest_HandlesExceptions(ExceptionType exceptionType)
	{
		Exception exception = exceptionType == ExceptionType.GeneralException
			? new InvalidOperationException("fail")
			: new TaskCanceledException("Canceled!");

		FakeHttpMessageHandler handler = new() { ThrowOnSend = exception };
		HttpClient client = new(handler);
		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			ExpectTaskCancellation = exceptionType == ExceptionType.TaskCanceledExpected
		};

		StreamingRestObject<string> result = await client.StreamingRestObjectRequest<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.Response.ShouldBeNull();
	}

	[Theory]
	[InlineData(1000000, 100, 8192)]
	[InlineData(100000, 10, 8192)]
	[InlineData(10000, 1, 8192)]
	[InlineData(90000000, 10000, 8192)]
	public void GetChunkingParameters_ReturnsExpectedValues(int itemCount, int startingChunk, int bufferLimit)
	{
		(int itemsPerChunk, int numberOfChunks) = RestHelpersStatic.GetChunkingParameters(itemCount, startingChunk, bufferLimit);

		itemsPerChunk.ShouldBeGreaterThan(0);
		numberOfChunks.ShouldBeGreaterThan(0);
		numberOfChunks.ShouldBeLessThan(bufferLimit);
	}

	[Fact]
	public async Task HandleResponse_ReturnsSuccessResult()
	{
		HttpResponseMessage response = new(HttpStatusCode.OK)
		{
			Content = new StringContent("\"success\"", Encoding.UTF8, "application/json")
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			LogResponse = true
		};

		string? result = await response.HandleResponse<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe("success");
	}

	[Fact]
	public async Task HandleResponse_HandlesErrorResponse()
	{
		HttpResponseMessage response = new(HttpStatusCode.BadRequest)
		{
			Content = new StringContent("Error message", Encoding.UTF8, "text/plain")
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			LogQuery = true
		};

		string? result = await response.HandleResponse<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task HandleResponse_HandlesProblemDetails()
	{
		string problemJson = JsonSerializer.Serialize(new
		{
			status = 400,
			title = "Bad Request",
			errors = new Dictionary<string, string[]>
						{
								{ "field1", valueArray }
						}
		});

		HttpResponseMessage response = new(HttpStatusCode.BadRequest)
		{
			Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get
		};

		string? result = await response.HandleResponse<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task HandleResponse_HandlesException()
	{
		HttpResponseMessage response = new(HttpStatusCode.OK)
		{
			Content = new FakeContent(true)
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get
		};

		string? result = await response.HandleResponse<string, string>(options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task HandleResponseAsync_YieldsSuccessResults()
	{
		HttpResponseMessage response = new(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"a\",\"b\"]", Encoding.UTF8, "application/json")
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			LogResponse = true
		};

		List<string?> results = new();
		await foreach (string? item in response.HandleResponseAsync<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task HandleResponseAsync_HandlesGZipEncoding()
	{
		byte[] jsonBytes = Encoding.UTF8.GetBytes("[\"a\",\"b\"]");
		MemoryStream compressedStream = new();
		await using (System.IO.Compression.GZipStream gzip = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true))
		{
			await gzip.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
		}
		compressedStream.Position = 0;

		HttpResponseMessage response = new(HttpStatusCode.OK)
		{
			Content = new StreamContent(compressedStream)
		};
		response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		response.Content.Headers.ContentEncoding.Add("gzip");

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get
		};

		List<string?> results = new();
		await foreach (string? item in response.HandleResponseAsync<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task HandleResponseAsync_HandlesBrotliEncoding()
	{
		byte[] jsonBytes = Encoding.UTF8.GetBytes("[\"a\",\"b\"]");
		MemoryStream compressedStream = new();
		await using (System.IO.Compression.BrotliStream brotli = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true))
		{
			await brotli.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
		}
		compressedStream.Position = 0;

		HttpResponseMessage response = new(HttpStatusCode.OK)
		{
			Content = new StreamContent(compressedStream)
		};
		response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		response.Content.Headers.ContentEncoding.Add("br");

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get
		};

		List<string?> results = new();
		await foreach (string? item in response.HandleResponseAsync<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task HandleResponseAsync_HandlesErrorResponse()
	{
		HttpResponseMessage response = new(HttpStatusCode.BadRequest)
		{
			Content = new StringContent("Error", Encoding.UTF8, "text/plain")
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get,
			LogResponse = true
		};

		List<string?> results = new();
		await foreach (string? item in response.HandleResponseAsync<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBeEmpty();
	}

	private static readonly string[] value = new[] { "error1" };
	private static readonly string[] valueArray = new[] { "error1", "error2" };

	[Fact]
	public async Task HandleResponseAsync_HandlesProblemDetails()
	{
		string problemJson = JsonSerializer.Serialize(new
		{
			status = 400,
			title = "Bad Request",
			errors = new Dictionary<string, string[]>
				{
						{ "field1", value }
				}
		});

		HttpResponseMessage response = new(HttpStatusCode.BadRequest)
		{
			Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json")
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get
		};

		List<string?> results = new();
		await foreach (string? item in response.HandleResponseAsync<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task HandleResponseAsync_HandlesException()
	{
		HttpResponseMessage response = new(HttpStatusCode.OK)
		{
			Content = new FakeContent(true)
		};

		RequestOptions<string> options = new()
		{
			Url = "http://test",
			HttpMethod = HttpMethod.Get
		};

		List<string?> results = new();
		await foreach (string? item in response.HandleResponseAsync<string, string>(options, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("application/json", null, false)]
	[InlineData("application/json", "gzip", false)]
	[InlineData("application/json", "br", false)]
	[InlineData("application/json", null, true)]
	[InlineData("text/plain", null, false)]
	public async Task ReadResponseStream_HandlesContentTypes(string contentType, string? encoding, bool useNewtonsoft)
	{
		// For JSON, the test data should be a JSON string (with quotes)
		// For text/plain, the test data should be plain text (without quotes)
		string testData = contentType.Contains("json") ? "\"test data\"" : "test data";
		MemoryStream stream;

		// If encoding is specified, compress the data appropriately
		if (encoding == "gzip")
		{
			byte[] jsonBytes = Encoding.UTF8.GetBytes(testData);
			stream = new();
			await using (System.IO.Compression.GZipStream gzip = new(stream, System.IO.Compression.CompressionMode.Compress, true))
			{
				await gzip.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
			}
			stream.Position = 0;
		}
		else if (encoding == "br")
		{
			byte[] jsonBytes = Encoding.UTF8.GetBytes(testData);
			stream = new();
			await using (System.IO.Compression.BrotliStream brotli = new(stream, System.IO.Compression.CompressionMode.Compress, true))
			{
				await brotli.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
			}
			stream.Position = 0;
		}
		else
		{
			stream = new(Encoding.UTF8.GetBytes(testData));
		}

		string? result = await stream.ReadResponseStream<string>(contentType, encoding, useNewtonsoft, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe("test data");
	}

	[Fact]
	public async Task ReadResponseStream_HandlesEmptyStream()
	{
		MemoryStream stream = new(Array.Empty<byte>());

		string? result = await stream.ReadResponseStream<string>("application/json", null, false, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(CompressionType.GZip)]
	[InlineData(CompressionType.Brotli)]
	public async Task ReadResponseStream_HandlesJsonWithCompression(CompressionType compressionType)
	{
		byte[] jsonBytes = Encoding.UTF8.GetBytes("\"test\"");
		MemoryStream compressedStream = new();

		if (compressionType == CompressionType.GZip)
		{
			await using System.IO.Compression.GZipStream gzip = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true);
			await gzip.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
		}
		else
		{
			await using System.IO.Compression.BrotliStream brotli = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true);
			await brotli.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
		}
		compressedStream.Position = 0;

		string encoding = compressionType == CompressionType.GZip ? "gzip" : "br";
		string? result = await compressedStream.ReadResponseStream<string>("application/json", encoding, false, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe("test");
	}

	[Theory]
	[InlineData(CompressionType.GZip)]
	[InlineData(CompressionType.Brotli)]
	public async Task ReadResponseStream_HandlesTextWithCompression(CompressionType compressionType)
	{
		byte[] textBytes = Encoding.UTF8.GetBytes("plain text");
		MemoryStream compressedStream = new();

		if (compressionType == CompressionType.GZip)
		{
			await using System.IO.Compression.GZipStream gzip = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true);
			await gzip.WriteAsync(textBytes, TestContext.Current.CancellationToken);
		}
		else
		{
			await using System.IO.Compression.BrotliStream brotli = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true);
			await brotli.WriteAsync(textBytes, TestContext.Current.CancellationToken);
		}
		compressedStream.Position = 0;

		string encoding = compressionType == CompressionType.GZip ? "gzip" : "br";
		string? result = await compressedStream.ReadResponseStream<string>("text/plain", encoding, false, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe("plain text");
	}

	[Fact]
	public async Task ReadResponseStream_HandlesMessagePack()
	{
		TestModel model = new() { Name = "Test", Value = 123 };
		byte[] msgPackBytes = MessagePackSerializer.Serialize(model, cancellationToken: TestContext.Current.CancellationToken);
		MemoryStream stream = new(msgPackBytes);

		TestModel? result = await stream.ReadResponseStream<TestModel>("application/x-msgpack", null, false, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Name.ShouldBe("Test");
		result.Value.ShouldBe(123);
	}

	[Fact]
	public async Task ReadResponseStream_HandlesMessagePackWithCompression()
	{
		TestModel model = new() { Name = "Test", Value = 123 };
		byte[] msgPackBytes = MessagePackSerializer.Serialize(model, cancellationToken: TestContext.Current.CancellationToken);
		MemoryStream stream = new(msgPackBytes);

		MsgPackOptions options = new() { UseMsgPackCompression = true };

		TestModel? result = await stream.ReadResponseStream<TestModel>("application/x-msgpack", null, false, msgPackOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReadResponseStream_HandlesMessagePackWithUntrusted()
	{
		TestModel model = new() { Name = "Test", Value = 123 };
		byte[] msgPackBytes = MessagePackSerializer.Serialize(model, cancellationToken: TestContext.Current.CancellationToken);
		MemoryStream stream = new(msgPackBytes);

		MsgPackOptions options = new() { UseMsgPackUntrusted = true };

		TestModel? result = await stream.ReadResponseStream<TestModel>("application/x-msgpack", null, false, msgPackOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReadResponseStream_HandlesException()
	{
		MemoryStream stream = new(Encoding.UTF8.GetBytes("invalid json"));

		TestModel? result = await stream.ReadResponseStream<TestModel>("application/json", null, false, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task ReadResponseStreamAsync_YieldsJsonResults()
	{
		const string json = "[\"a\",\"b\",\"c\"]";
		MemoryStream stream = new(Encoding.UTF8.GetBytes(json));

		List<string?> results = new();
		await foreach (string? item in stream.ReadResponseStreamAsync<string>("application/json", null, null, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b", "c"]);
	}

	[Theory]
	[InlineData(CompressionType.GZip)]
	[InlineData(CompressionType.Brotli)]
	public async Task ReadResponseStreamAsync_HandlesCompression(CompressionType compressionType)
	{
		byte[] jsonBytes = Encoding.UTF8.GetBytes("[\"a\",\"b\"]");
		MemoryStream compressedStream = new();

		if (compressionType == CompressionType.GZip)
		{
			await using System.IO.Compression.GZipStream gzip = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true);
			await gzip.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
		}
		else
		{
			await using System.IO.Compression.BrotliStream brotli = new(compressedStream, System.IO.Compression.CompressionMode.Compress, true);
			await brotli.WriteAsync(jsonBytes, TestContext.Current.CancellationToken);
		}
		compressedStream.Position = 0;

		string encoding = compressionType == CompressionType.GZip ? "gzip" : "br";
		List<string?> results = new();
		await foreach (string? item in compressedStream.ReadResponseStreamAsync<string>("application/json", encoding, null, cancellationToken: TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task ReadResponseStreamAsync_ThrowsForNonJsonContentType()
	{
		MemoryStream stream = new(Encoding.UTF8.GetBytes("data"));

		await Should.ThrowAsync<NotImplementedException>(async () =>
		{
			await foreach (string? _ in stream.ReadResponseStreamAsync<string>("application/xml", null, null))
			{
				// No-op
			}
		});
	}

	[Theory]
	[InlineData("POST", "application/json")]
	[InlineData("PUT", "application/json")]
	[InlineData("PATCH", null)]
	[InlineData("GET", null)]
	public void AddContent_AddsContentForHttpMethods(string methodString, string? expectedContentType)
	{
		HttpMethod method = new(methodString);
		HttpRequestMessage request = new(method, "http://test");
		TestModel model = new() { Name = "Test", Value = 42 };

		if (method == HttpMethod.Patch)
		{
			StringContent patchContent = new("[{\"op\":\"replace\",\"path\":\"/name\",\"value\":\"New\"}]");
			request.AddContent(method, null, model, patchContent);
			request.Content.ShouldNotBeNull();
			request.Content!.Headers.ContentType?.MediaType.ShouldBe("application/json-patch+json");
		}
		else
		{
			request.AddContent(method, null, model, null);

			if (method == HttpMethod.Get)
			{
				request.Content.ShouldBeNull();
			}
			else
			{
				request.Content.ShouldNotBeNull();
				request.Content!.Headers.ContentType?.MediaType.ShouldBe(expectedContentType);
			}
		}
	}

	[Fact]
	public void AddContent_HandlesMemoryPack()
	{
		HttpRequestMessage request = new(HttpMethod.Post, "http://test");
		Dictionary<string, string> headers = new() { { "Content-Type", "application/x-memorypack" } };
		TestModel model = new() { Name = "Test", Value = 42 };

		request.AddContent(HttpMethod.Post, headers, model, null);

		request.Content.ShouldNotBeNull();
		request.Content!.Headers.ContentType?.MediaType.ShouldBe("application/x-memorypack");
	}

	[Fact]
	public void AddContent_HandlesMessagePack()
	{
		HttpRequestMessage request = new(HttpMethod.Post, "http://test");
		Dictionary<string, string> headers = new() { { "Content-Type", "application/x-msgpack" } };
		TestModel model = new() { Name = "Test", Value = 42 };

		request.AddContent(HttpMethod.Post, headers, model, null);

		request.Content.ShouldNotBeNull();
		request.Content!.Headers.ContentType?.MediaType.ShouldBe("application/x-msgpack");
	}

	[Theory]
	[InlineData("token123")]
	[InlineData(null)]
	public void AttachHeaders_AddsBearerToken(string? token)
	{
		HttpRequestMessage request = new(HttpMethod.Get, "http://test");

		request.AttachHeaders(token, null);

		if (token != null)
		{
			request.Headers.Authorization.ShouldNotBeNull();
			request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
			request.Headers.Authorization.Parameter.ShouldBe(token);
		}
	}

	[Fact]
	public void AttachHeaders_AddsCustomHeaders()
	{
		HttpRequestMessage request = new(HttpMethod.Get, "http://test");
		Dictionary<string, string> headers = new()
				{
						{ "X-Custom-Header", "value1" },
						{ "X-Another-Header", "value2" }
				};

		request.AttachHeaders(null, headers);

		request.Headers.Contains("X-Custom-Header").ShouldBeTrue();
		request.Headers.Contains("X-Another-Header").ShouldBeTrue();
	}

	[Fact]
	public void AttachHeaders_HandlesNullHeaders()
	{
		HttpRequestMessage request = new(HttpMethod.Get, "http://test");

		request.AttachHeaders("token", null);

		request.Headers.Authorization.ShouldNotBeNull();
	}

	[Fact]
	public void AttachHeaders_HandlesEmptyBearerTokenWithoutAuthHeader()
	{
		HttpRequestMessage request = new(HttpMethod.Get, "http://test");

		request.AttachHeaders(string.Empty, null);

		request.Headers.Authorization.ShouldBeNull();
	}

	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		public HttpResponseMessage? Response { get; set; }

		public Exception? ThrowOnSend { get; set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (ThrowOnSend is not null)
			{
				throw ThrowOnSend;
			}

			return Task.FromResult(Response ?? new HttpResponseMessage(HttpStatusCode.OK));
		}
	}

	private sealed class FakeContent(bool throwOnRead) : HttpContent
	{
		private readonly bool throwOnRead = throwOnRead;

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
		{
			if (throwOnRead)
			{
				throw new InvalidOperationException("Fake error");
			}
			return Task.CompletedTask;
		}

		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return false;
		}
	}
}

[MemoryPackable]
[MessagePackObject(true, AllowPrivate = true)]
internal partial class TestModel
{
	[Key(0)]
	public string Name { get; set; } = string.Empty;

	[Key(1)]
	public int Value { get; set; }
}

