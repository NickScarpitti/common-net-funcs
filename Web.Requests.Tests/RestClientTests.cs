using System.Net;
using System.Text;
using CommonNetFuncs.Web.Requests.Rest;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

namespace Web.Requests.Tests;

public sealed class RestClientTests
{
	[Fact]
	public void RestClient_Constructor_ThrowsWhenHttpClientIsNull()
	{
		// Arrange, Act & Assert

		Should.Throw<ArgumentNullException>(() => new RestClient(null!)).ParamName.ShouldBe("httpClient");
	}

	[Fact]
	public void RestClient_Constructor_AcceptsValidHttpClient()
	{
		// Arrange

		using HttpClient httpClient = new();

		// Act

		RestClient restClient = new(httpClient);

		// Assert

		restClient.ShouldNotBeNull();
		restClient.BaseAddress.ShouldBeNull(); // Default HttpClient has no base address

	}

	[Fact]
	public void RestClient_BaseAddress_ReturnsHttpClientBaseAddress()
	{
		// Arrange

		Uri baseAddress = new("https://api.example.com");
		using HttpClient httpClient = new() { BaseAddress = baseAddress };

		// Act

		RestClient restClient = new(httpClient);

		// Assert

		restClient.BaseAddress.ShouldBe(baseAddress);
	}

	[Fact]
	public async Task RestClient_RestObjectRequest_CallsExtensionMethod()
	{
		// Arrange

		FakeHttpMessageHandler handler = new() { Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"value\":42}", Encoding.UTF8, "application/json") } };

		using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com") };
		RestClient restClient = new(httpClient);

		RequestOptions<object> options = new()
		{
			Url = "https://api.test.com/endpoint",
			HttpMethod = HttpMethod.Get
		};

		// Act

		RestObject<TestModel> result = await restClient.RestObjectRequest<TestModel, object>(options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Response.ShouldNotBeNull();
		result.Response!.StatusCode.ShouldBe(HttpStatusCode.OK);
	}

	[Fact]
	public async Task RestClient_RestObjectRequest_HandlesError()
	{
		// Arrange

		FakeHttpMessageHandler handler = new()
		{
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Error", Encoding.UTF8, "text/plain") }
		};

		using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com") };
		RestClient restClient = new(httpClient);

		RequestOptions<object> options = new() { Url = "https://api.test.com/endpoint", HttpMethod = HttpMethod.Get };

		// Act

		RestObject<TestModel> result = await restClient.RestObjectRequest<TestModel, object>(options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Response.ShouldNotBeNull();
		result.Response!.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task RestClient_StreamingRestObjectRequest_CallsExtensionMethod()
	{
		// Arrange

		const string jsonArray = "[{\"value\":1},{\"value\":2},{\"value\":3}]";
		FakeHttpMessageHandler handler = new() { Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonArray, Encoding.UTF8, "application/json") } };

		using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com") };
		RestClient restClient = new(httpClient);

		RequestOptions<object> options = new() { Url = "https://api.test.com/streaming", HttpMethod = HttpMethod.Get };

		// Act

		StreamingRestObject<TestModel> result = await restClient.StreamingRestObjectRequest<TestModel, object>(options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Response.ShouldNotBeNull();
		result.Response!.StatusCode.ShouldBe(HttpStatusCode.OK);
		result.Result.ShouldNotBeNull();

		// Enumerate the results

		List<TestModel?> items = new();
		await foreach (TestModel? item in result.Result!)
		{
			items.Add(item);
		}
		items.Count.ShouldBe(3);
		items[0]!.Value.ShouldBe(1);
		items[1]!.Value.ShouldBe(2);
		items[2]!.Value.ShouldBe(3);
	}

	[Fact]
	public async Task RestClient_StreamingRestObjectRequest_HandlesEmptyStream()
	{
		// Arrange

		FakeHttpMessageHandler handler = new() { Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") } };

		using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com") };
		RestClient restClient = new(httpClient);

		RequestOptions<object> options = new() { Url = "https://api.test.com/streaming", HttpMethod = HttpMethod.Get };

		// Act

		StreamingRestObject<TestModel> result = await restClient.StreamingRestObjectRequest<TestModel, object>(options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldNotBeNull();

		// Enumerate the results

		List<TestModel?> items = new();
		await foreach (TestModel? item in result.Result!) { items.Add(item); }
		items.ShouldBeEmpty();
	}

	private class TestModel
	{
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
		public int Value { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
	}
}

public sealed class RestClientFactoryTests
{
	[Fact]
	public void RestClientFactory_Constructor_ThrowsWhenHttpClientFactoryIsNull()
	{
		// Arrange, Act & Assert

		Should.Throw<ArgumentNullException>(() => new RestClientFactory(null!)).ParamName.ShouldBe("httpClientFactory");
	}

	[Fact]
	public void RestClientFactory_Constructor_AcceptsValidHttpClientFactory()
	{
		// Arrange

		IHttpClientFactory factory = A.Fake<IHttpClientFactory>();

		// Act

		RestClientFactory restClientFactory = new(factory);

		// Assert

		restClientFactory.ShouldNotBeNull();
	}

	[Fact]
	public void RestClientFactory_CreateClient_ReturnsRestClient()
	{
		// Arrange

		using HttpClient httpClient = new();
		IHttpClientFactory factory = A.Fake<IHttpClientFactory>();
		A.CallTo(() => factory.CreateClient("TestApi")).Returns(httpClient);

		RestClientFactory restClientFactory = new(factory);

		// Act

		IRestClient restClient = restClientFactory.CreateClient("TestApi");

		// Assert

		restClient.ShouldNotBeNull();
		restClient.ShouldBeOfType<RestClient>();
		A.CallTo(() => factory.CreateClient("TestApi")).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void RestClientFactory_CreateClient_PassesApiNameToHttpClientFactory()
	{
		// Arrange

		using HttpClient httpClient = new();
		IHttpClientFactory factory = A.Fake<IHttpClientFactory>();
		A.CallTo(() => factory.CreateClient(A<string>._)).Returns(httpClient);

		RestClientFactory restClientFactory = new(factory);

		// Act

		IRestClient restClient1 = restClientFactory.CreateClient("Api1");
		IRestClient restClient2 = restClientFactory.CreateClient("Api2");

		// Assert

		restClient1.ShouldNotBeNull();
		restClient2.ShouldNotBeNull();
		A.CallTo(() => factory.CreateClient("Api1")).MustHaveHappenedOnceExactly();
		A.CallTo(() => factory.CreateClient("Api2")).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void RestClientFactory_CreateClient_ReturnsNewInstanceEachTime()
	{
		// Arrange

		IHttpClientFactory factory = A.Fake<IHttpClientFactory>();
		A.CallTo(() => factory.CreateClient(A<string>._)).ReturnsLazily(() => new HttpClient());

		RestClientFactory restClientFactory = new(factory);

		// Act

		IRestClient restClient1 = restClientFactory.CreateClient("TestApi");
		IRestClient restClient2 = restClientFactory.CreateClient("TestApi");

		// Assert

		restClient1.ShouldNotBeNull();
		restClient2.ShouldNotBeNull();
		restClient1.ShouldNotBe(restClient2); // Different instances

	}

	[Fact]
	public void RestClientFactory_CreateClient_PropagatesBaseAddress()
	{
		// Arrange

		Uri baseAddress = new("https://api.example.com");
		using HttpClient httpClient = new() { BaseAddress = baseAddress };

		IHttpClientFactory factory = A.Fake<IHttpClientFactory>();
		A.CallTo(() => factory.CreateClient("TestApi")).Returns(httpClient);

		RestClientFactory restClientFactory = new(factory);

		// Act

		IRestClient restClient = restClientFactory.CreateClient("TestApi");

		// Assert

		restClient.BaseAddress.ShouldBe(baseAddress);
	}
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
	public HttpResponseMessage? Response { get; set; }
	public Exception? ThrowOnSend { get; set; }

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (ThrowOnSend != null)
		{
			throw ThrowOnSend;
		}

		return Task.FromResult(Response ?? new HttpResponseMessage(HttpStatusCode.OK));
	}
}
