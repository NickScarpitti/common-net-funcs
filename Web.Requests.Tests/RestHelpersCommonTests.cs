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
}
