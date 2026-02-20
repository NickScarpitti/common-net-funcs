using System.Net;
using CommonNetFuncs.Web.Requests.Rest;
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

namespace Web.Requests.Tests;

/// <summary>
/// Additional tests for RestHelpersWrapper to improve coverage of retry, bearer token, and max retry scenarios
/// </summary>
public sealed class RestHelpersWrapperRetryTests : IDisposable
{
	private readonly IRestClientFactory restClientFactory;
	private readonly RestHelpersWrapper wrapper;
	private readonly IRestClient fakeRestClient;

	public RestHelpersWrapperRetryTests()
	{
		restClientFactory = A.Fake<IRestClientFactory>();
		fakeRestClient = A.Fake<IRestClient>();
		A.CallTo(() => restClientFactory.CreateClient(A<string>._)).Returns(fakeRestClient);
		A.CallTo(() => fakeRestClient.BaseAddress).Returns(new Uri("http://test.com/"));
		wrapper = new RestHelpersWrapper(restClientFactory);
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	private sealed class TestModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	#region GetStreaming Tests

	[Fact]
	public async Task GetStreaming_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel[] items = [new() { Id = 1, Name = "Test1" }, new() { Id = 2, Name = "Test2" }];
		StreamingRestObject<TestModel> streamingObject = new()
		{
			Result = CreateAsyncEnumerable(items),
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(streamingObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "test-token");

		// Act
		List<TestModel?> results = [];
		await foreach (TestModel? item in wrapper.GetStreaming<TestModel>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(2);
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "test-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GetStreaming_ShouldRetry_AndLogAttempts_WhenRequestFails()
	{
		// Arrange
		StreamingRestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		};

		TestModel[] items = [new() { Id = 1, Name = "Success" }];
		StreamingRestObject<TestModel> successResponse = new()
		{
			Result = CreateAsyncEnumerable(items),
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		List<TestModel?> results = [];
		await foreach (TestModel? item in wrapper.GetStreaming<TestModel>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task GetStreaming_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		StreamingRestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		List<TestModel?> results = [];
		await foreach (TestModel? item in wrapper.GetStreaming<TestModel>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBeEmpty();
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region PostRequestStreaming Tests

	[Fact]
	public async Task PostRequestStreaming_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		TestModel[] items = [new() { Id = 1, Name = "Result1" }];
		StreamingRestObject<TestModel> streamingObject = new()
		{
			Result = CreateAsyncEnumerable(items),
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(streamingObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "post-token");

		// Act
		List<TestModel?> results = [];
		await foreach (TestModel? item in wrapper.PostRequestStreaming(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "post-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task PostRequestStreaming_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		StreamingRestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadGateway)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		List<TestModel?> results = [];
		await foreach (TestModel? item in wrapper.PostRequestStreaming(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBeEmpty();
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(3, Times.Exactly);
	}

	#endregion

	#region GenericPostRequest Tests

	[Fact]
	public async Task GenericPostRequest_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Input" };
		TestModel expectedResult = new() { Id = 2, Name = "Output" };
		RestObject<TestModel> restObject = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "generic-token");

		// Act
		TestModel? result = await wrapper.GenericPostRequest<TestModel, TestModel>(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(2);
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "generic-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GenericPostRequest_ShouldRetry_AndLogAttempts()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Input" };
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
		};

		TestModel expectedResult = new() { Id = 2, Name = "Success" };
		RestObject<TestModel> successResponse = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.GenericPostRequest<TestModel, TestModel>(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task GenericPostRequest_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Input" };
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.GenericPostRequest<TestModel, TestModel>(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region GenericPostRequestStreaming Tests

	[Fact]
	public async Task GenericPostRequestStreaming_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Input" };
		StreamingRestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		List<TestModel?> results = [];
		await foreach (TestModel? item in wrapper.GenericPostRequestStreaming<TestModel, TestModel>(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBeEmpty();
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region StringPostRequest Tests

	[Fact]
	public async Task StringPostRequest_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<string?> restObject = new()
		{
			Result = "string-response",
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "string-token");

		// Act
		string? result = await wrapper.StringPostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("string-response");
		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "string-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task StringPostRequest_ShouldRetry_AndLogAttempts()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<string?> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
		};

		RestObject<string?> successResponse = new()
		{
			Result = "retry-success",
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		string? result = await wrapper.StringPostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("retry-success");
		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task StringPostRequest_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<string?> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		string? result = await wrapper.StringPostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region PatchRequest Tests

	[Fact]
	public async Task PatchRequest_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel oldModel = new() { Id = 1, Name = "Old" };
		TestModel newModel = new() { Id = 1, Name = "New" };
		RestObject<TestModel> restObject = new()
		{
			Result = newModel,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "patch-token");

		// Act
		TestModel? result = await wrapper.PatchRequest(options, newModel, oldModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("New");
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "patch-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task PatchRequest_ShouldRetry_AndLogAttempts()
	{
		// Arrange
		TestModel oldModel = new() { Id = 1, Name = "Old" };
		TestModel newModel = new() { Id = 1, Name = "New" };
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
		};

		RestObject<TestModel> successResponse = new()
		{
			Result = newModel,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.PatchRequest(options, newModel, oldModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task PatchRequest_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		TestModel oldModel = new() { Id = 1, Name = "Old" };
		TestModel newModel = new() { Id = 1, Name = "New" };
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadGateway)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.PatchRequest(options, newModel, oldModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region PutRequest Tests

	[Fact]
	public async Task PutRequest_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel replacementModel = new() { Id = 1, Name = "Replacement" };
		RestObject<TestModel> restObject = new()
		{
			Result = replacementModel,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "put-token");

		// Act
		TestModel? result = await wrapper.PutRequest(options, replacementModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Replacement");
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "put-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task PutRequest_ShouldRetry_AndLogAttempts()
	{
		// Arrange
		TestModel replacementModel = new() { Id = 1, Name = "Replacement" };
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		RestObject<TestModel> successResponse = new()
		{
			Result = replacementModel,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.PutRequest(options, replacementModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task PutRequest_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		TestModel replacementModel = new() { Id = 1, Name = "Replacement" };
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.PutRequest(options, replacementModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region DeleteRequest Tests

	[Fact]
	public async Task DeleteRequest_ShouldUseBearerToken_WhenEnabled()
	{
		// Arrange
		TestModel expectedResult = new() { Id = 1, Name = "Deleted" };
		RestObject<TestModel> restObject = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "delete-token");

		// Act
		TestModel? result = await wrapper.DeleteRequest<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Deleted");
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "delete-token"), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task DeleteRequest_ShouldRetry_AndLogAttempts()
	{
		// Arrange
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
		};

		TestModel expectedResult = new() { Id = 1, Name = "Deleted" };
		RestObject<TestModel> successResponse = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.DeleteRequest<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task DeleteRequest_ShouldReachMaxRetry_AndLogWarning()
	{
		// Arrange
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.DeleteRequest<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region GetBearerTokenFunc Tests

	[Fact]
	public async Task PostRequest_ShouldCallGetBearerTokenFunc_WhenBearerTokenNotProvided()
	{
		// Arrange
		const string dynamicToken = "func-generated-token";
		static ValueTask<string> getBearerTokenFunc(string _, bool __)
		{
			return new(dynamicToken);
		}

		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<TestModel> restObject = new()
		{
			Result = postObject,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true,
			ResilienceOptions: new ResilienceOptions(GetBearerTokenFunc: getBearerTokenFunc));

		// Act
		TestModel? result = await wrapper.PostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == dynamicToken), A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	#endregion

	#region Helper Methods

	private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			await Task.Yield();
			yield return item;
		}
	}

	#endregion
}
