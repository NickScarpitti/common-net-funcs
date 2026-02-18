using System.Net;
using System.Text;
using CommonNetFuncs.Web.Requests.Rest;

namespace Web.Requests.Tests;

public sealed class RestHelpersCommonTests
{
	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		public HttpResponseMessage? Response { get; set; }

#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
		public Exception? ThrowOnSend { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (ThrowOnSend is not null)
			{
				throw ThrowOnSend;
			}

			return Task.FromResult(Response ?? new HttpResponseMessage(HttpStatusCode.OK));
		}
	}

	[Fact]
	public async Task RestRequest_DelegatesToStaticExtension()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient httpClient = new(handler);
		RestHelpersCommon restHelpers = new(httpClient);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"result\"", Encoding.UTF8, "application/json")
		};

		string? result = await restHelpers.RestRequest<string, string>(options, TestContext.Current.CancellationToken);

		result.ShouldBe("result");
	}

	public class StringWrapper
	{
		public string? Value { get; set; }
	}

	[Fact]
	public async Task StreamingRestRequest_DelegatesToStaticExtension()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient httpClient = new(handler);
		RestHelpersCommon restHelpers = new(httpClient);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		const string json = "[{\"Value\":\"a\"},{\"Value\":\"b\"}]";
		StringContent content = new(json, Encoding.UTF8, "application/json");
		content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);
		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = content
		};

		List<string?> results = new();
		await foreach (StringWrapper? item in restHelpers.StreamingRestRequest<StringWrapper, string>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item?.Value);
		}
		results.ShouldBe(["a", "b"]);
	}

	[Fact]
	public async Task RestRequestObject_DelegatesToStaticExtension()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient httpClient = new(handler);
		RestHelpersCommon restHelpers = new(httpClient);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"foo\"", Encoding.UTF8, "application/json")
		};

		RestObject<string> result = await restHelpers.RestRequestObject<string, string>(options, TestContext.Current.CancellationToken);

		result.Result.ShouldBe("foo");
		result.Response.ShouldNotBeNull();
	}

	[Fact]
	public async Task StreamingRestRequestObject_DelegatesToStaticExtension()
	{
		FakeHttpMessageHandler handler = new();
		HttpClient httpClient = new(handler);
		RestHelpersCommon restHelpers = new(httpClient);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"x\",\"y\"]", Encoding.UTF8, "application/json")
		};

		StreamingRestObject<string> result = await restHelpers.StreamingRestRequestObject<string, string>(options, TestContext.Current.CancellationToken);

		List<string?> items = new();
		await foreach (string? item in result.Result!)
		{
			items.Add(item);
		}
		items.ShouldBe(["x", "y"]);
	}

	#region RestHelpersCommonFactory Tests

	[Fact]
	public async Task RestHelpersCommonFactory_RestRequest_WithDefaultClient()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);
		RestHelpersCommonFactory restHelpers = new(factory);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"factory-result\"", Encoding.UTF8, "application/json")
		};

		string? result = await restHelpers.RestRequest<string, string>(options, TestContext.Current.CancellationToken);

		result.ShouldBe("factory-result");
	}

	[Fact]
	public async Task RestHelpersCommonFactory_RestRequest_WithNamedClient()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, "named-client");
		RestHelpersCommonFactory restHelpers = new(factory, "named-client");
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"named-result\"", Encoding.UTF8, "application/json")
		};

		string? result = await restHelpers.RestRequest<string, string>(options, TestContext.Current.CancellationToken);

		result.ShouldBe("named-result");
		factory.LastRequestedClientName.ShouldBe("named-client");
	}

	[Fact]
	public async Task RestHelpersCommonFactory_StreamingRestRequest_WithDefaultClient()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);
		RestHelpersCommonFactory restHelpers = new(factory);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		const string json = "[{\"Value\":\"factory-a\"},{\"Value\":\"factory-b\"}]";
		StringContent content = new(json, Encoding.UTF8, "application/json");
		content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);
		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = content
		};

		List<string?> results = new();
		await foreach (StringWrapper? item in restHelpers.StreamingRestRequest<StringWrapper, string>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item?.Value);
		}
		results.ShouldBe(["factory-a", "factory-b"]);
	}

	[Fact]
	public async Task RestHelpersCommonFactory_RestRequestObject_WithDefaultClient()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);
		RestHelpersCommonFactory restHelpers = new(factory);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("\"factory-object\"", Encoding.UTF8, "application/json")
		};

		RestObject<string> result = await restHelpers.RestRequestObject<string, string>(options, TestContext.Current.CancellationToken);

		result.Result.ShouldBe("factory-object");
		result.Response.ShouldNotBeNull();
	}

	[Fact]
	public async Task RestHelpersCommonFactory_StreamingRestRequestObject_WithDefaultClient()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);
		RestHelpersCommonFactory restHelpers = new(factory);
		RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

		handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("[\"factory-x\",\"factory-y\"]", Encoding.UTF8, "application/json")
		};

		StreamingRestObject<string> result = await restHelpers.StreamingRestRequestObject<string, string>(options, TestContext.Current.CancellationToken);

		List<string?> items = new();
		await foreach (string? item in result.Result!)
		{
			items.Add(item);
		}
		items.ShouldBe(["factory-x", "factory-y"]);
	}

	[Fact]
	public void RestHelpersCommonFactory_Dispose_DisposesHttpClient()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);
		RestHelpersCommonFactory restHelpers = new(factory);

		// Verify client is accessible before dispose
		restHelpers.client.ShouldNotBeNull();

		// Dispose
		restHelpers.Dispose();

		// Verify dispose was called (attempting to send request should throw)
		Should.Throw<ObjectDisposedException>(() => restHelpers.client.Send(new HttpRequestMessage(HttpMethod.Get, "http://test")));
	}

	[Fact]
	public void RestHelpersCommonFactory_Dispose_CanBeCalledMultipleTimes()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);
		RestHelpersCommonFactory restHelpers = new(factory);

		// Should not throw when called multiple times
		Should.NotThrow(() =>
		{
			restHelpers.Dispose();
			restHelpers.Dispose();
			restHelpers.Dispose();
		});
	}

	[Fact]
	public void RestHelpersCommonFactory_Finalizer_CallsDispose()
	{
		FakeHttpMessageHandler handler = new();
		FakeHttpClientFactory factory = new(handler, null);

		// Create in separate scope to allow GC
		WeakReference CreateAndForget()
		{
			RestHelpersCommonFactory restHelpers = new(factory);
			return new WeakReference(restHelpers);
		}

		WeakReference weakRef = CreateAndForget();

		// Force garbage collection to trigger finalizer
#pragma warning disable S1215 // Refactor the code to remove this use of 'GC.Collect'
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
#pragma warning restore S1215 // Refactor the code to remove this use of 'GC.Collect'

		// Object should be collected
		weakRef.IsAlive.ShouldBeFalse();
	}

	#endregion

	private sealed class FakeHttpClientFactory(FakeHttpMessageHandler handler, string? expectedName) : IHttpClientFactory
	{
		public string? LastRequestedClientName { get; private set; }

		public HttpClient CreateClient(string name)
		{
			LastRequestedClientName = name;
			if (expectedName != null && name != expectedName)
			{
				throw new InvalidOperationException($"Expected client name '{expectedName}' but got '{name}'");
			}
			return new HttpClient(handler);
		}
	}
}
