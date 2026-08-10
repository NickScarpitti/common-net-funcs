using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommonNetFuncs.Web.Requests.Rest;
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;
using FakeItEasy;
using MessagePack;
using xRetry.v3;

namespace Web.Requests.Tests;

public sealed class RestHelpersWrapperTests : IDisposable
{
	private readonly IRestClientFactory restClientFactory;
	private readonly RestHelpersWrapper wrapper;
	private readonly IRestClient fakeRestClient;

	public RestHelpersWrapperTests()
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

	#region Test Models

	private sealed class TestModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public decimal Value { get; set; }
	}

	#endregion

	#region GET Tests

	[Fact]
	public async Task Get_ShouldReturnResult_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel expectedResult = new() { Id = 1, Name = "Test", Value = 10.5m };
		RestObject<TestModel> restObject = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
		result.Value.ShouldBe(10.5m);
	}

	[Fact]
	public async Task Get_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		RestObject<TestModel> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task Get_ShouldRetry_WhenInitialRequestFails()
	{
		// Arrange
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		TestModel expectedResult = new() { Id = 1, Name = "Test" };
		RestObject<TestModel> successResponse = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public async Task Get_ShouldUseBearerToken_WhenProvided()
	{
		// Arrange
		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true, BearerToken: "test-token");

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
				A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "test-token"), A<CancellationToken>._))
				.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Get_ShouldCallGetBearerTokenFunc_WhenBearerTokenNotProvidedButFuncIs()
	{
		// Arrange
		const string expectedToken = "dynamic-token";
		static ValueTask<string> getBearerTokenFunc(string _, bool __)
		{
			return new(expectedToken);
		}

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true,
				ResilienceOptions: new ResilienceOptions(GetBearerTokenFunc: getBearerTokenFunc));

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
				A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == expectedToken), A<CancellationToken>._))
				.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Get_ShouldRefreshBearerToken_OnUnauthorized()
	{
		// Arrange
		bool tokenRefreshed = false;
		ValueTask<string> getBearerTokenFunc(string _, bool forceRefresh)
		{
			if (forceRefresh)
			{
				tokenRefreshed = true;
			}

			return new ValueTask<string>(forceRefresh ? "refreshed-token" : "initial-token");
		}

		RestObject<TestModel> unauthorizedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
		};

		RestObject<TestModel> successResponse = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.ReturnsNextFromSequence(Task.FromResult(unauthorizedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", UseBearerToken: true,
				ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10, GetBearerTokenFunc: getBearerTokenFunc));

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		tokenRefreshed.ShouldBeTrue();
	}

	[Fact]
	public async Task Get_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.Get<TestModel>(options));
	}

	[Fact]
	public async Task Get_ShouldInitializeResilienceOptions_WhenNull()
	{
		// Arrange
		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: null);

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		options.ResilienceOptions.ShouldNotBeNull();
	}

	#endregion

	#region GetStreaming Tests

	[Fact]
	public async Task GetStreaming_ShouldReturnResults_WhenRequestIsSuccessful()
	{
		// Arrange
		List<TestModel> expectedResults = new()
			{
				new TestModel { Id = 1, Name = "Test1" },
				new TestModel { Id = 2, Name = "Test2" }
			};

		StreamingRestObject<TestModel> streamingResponse = new()
		{
			Result = AsyncEnumerableFromList(expectedResults, TestContext.Current.CancellationToken),
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(streamingResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		List<TestModel?> results = new();
		await foreach (TestModel? item in wrapper.GetStreaming<TestModel>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(2);
		results[0]!.Id.ShouldBe(1);
		results[1]!.Id.ShouldBe(2);
	}

	[Fact]
	public async Task GetStreaming_ShouldReturnEmpty_WhenRequestFails()
	{
		// Arrange
		StreamingRestObject<TestModel> streamingResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(streamingResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		List<TestModel?> results = new();
		await foreach (TestModel? item in wrapper.GetStreaming<TestModel>(options, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetStreaming_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () =>
		{
			await foreach (TestModel? item in wrapper.GetStreaming<TestModel>(options))
			{
				// Should not reach here
			}
		});
	}

	#endregion

	#region POST Tests

	[Fact]
	public async Task PostRequest_ShouldReturnResult_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		TestModel expectedResult = new() { Id = 1, Name = "Test", Value = 10.5m };
		RestObject<TestModel> restObject = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.PostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Value.ShouldBe(10.5m);
	}

	[Fact]
	public async Task PostRequest_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<TestModel> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		TestModel? result = await wrapper.PostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task PostRequest_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.PostRequest(options, postObject));
	}

	#endregion

	#region PostRequestStreaming Tests

	[Fact]
	public async Task PostRequestStreaming_ShouldReturnResults_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		List<TestModel> expectedResults = new()
			{
				new TestModel { Id = 1, Name = "Result1" },
				new TestModel { Id = 2, Name = "Result2" }
			};

		StreamingRestObject<TestModel> streamingResponse = new()
		{
			Result = AsyncEnumerableFromList(expectedResults, TestContext.Current.CancellationToken),
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(streamingResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		List<TestModel?> results = new();
		await foreach (TestModel? item in wrapper.PostRequestStreaming(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(2);
		results[0]!.Id.ShouldBe(1);
		results[1]!.Id.ShouldBe(2);
	}

	[Fact]
	public async Task PostRequestStreaming_ShouldReturnEmpty_WhenRequestFails()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		StreamingRestObject<TestModel> streamingResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(streamingResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		List<TestModel?> results = new();
		await foreach (TestModel? item in wrapper.PostRequestStreaming(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task PostRequestStreaming_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () =>
		{
			await foreach (TestModel? item in wrapper.PostRequestStreaming(options, postObject))
			{
				// Should not reach here
			}
		});
	}

	#endregion

	#region GenericPostRequest Tests

	[Fact]
	public async Task GenericPostRequest_ShouldReturnResult_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		const string expectedResult = "Success";
		RestObject<string> restObject = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		string? result = await wrapper.GenericPostRequest<string, TestModel>(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe("Success");
	}

	[Fact]
	public async Task GenericPostRequest_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<string> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		string? result = await wrapper.GenericPostRequest<string, TestModel>(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GenericPostRequest_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		A.CallTo(() => fakeRestClient.RestObjectRequest<string, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.GenericPostRequest<string, TestModel>(options, postObject));
	}

	#endregion

	#region GenericPostRequestStreaming Tests

	[Fact]
	public async Task GenericPostRequestStreaming_ShouldReturnResults_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		List<string> expectedResults = new() { "Result1", "Result2" };

		StreamingRestObject<string> streamingResponse = new()
		{
			Result = AsyncEnumerableFromList(expectedResults, TestContext.Current.CancellationToken),
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<string, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(streamingResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		List<string?> results = new();
		await foreach (string? item in wrapper.GenericPostRequestStreaming<string, TestModel>(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(2);
		results[0].ShouldBe("Result1");
		results[1].ShouldBe("Result2");
	}

	[Fact]
	public async Task GenericPostRequestStreaming_ShouldReturnEmpty_WhenRequestFails()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		StreamingRestObject<string> streamingResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<string, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(streamingResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		List<string?> results = new();
		await foreach (string? item in wrapper.GenericPostRequestStreaming<string, TestModel>(options, postObject, TestContext.Current.CancellationToken))
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task GenericPostRequestStreaming_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		A.CallTo(() => fakeRestClient.StreamingRestObjectRequest<string, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () =>
		{
			await foreach (string? item in wrapper.GenericPostRequestStreaming<string, TestModel>(options, postObject))
			{
				// Should not reach here
			}
		});
	}

	#endregion

	#region StringPostRequest Tests

	[Fact]
	public async Task StringPostRequest_ShouldReturnResult_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		const string expectedResult = "Success Response";
		RestObject<string?> restObject = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._)).Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		string? result = await wrapper.StringPostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe("Success Response");
	}

	[Fact]
	public async Task StringPostRequest_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<string?> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		string? result = await wrapper.StringPostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task StringPostRequest_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel postObject = new() { Id = 1, Name = "Test" };
		A.CallTo(() => fakeRestClient.RestObjectRequest<string?, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.StringPostRequest(options, postObject));
	}

	#endregion

	#region PatchRequest Tests

	[Fact]
	public async Task PatchRequest_ShouldReturnResult_WhenChangesDetected()
	{
		// Arrange
		TestModel oldModel = new() { Id = 1, Name = "Old", Value = 10m };
		TestModel newModel = new() { Id = 1, Name = "New", Value = 20m };
		RestObject<TestModel> restObject = new()
		{
			Result = newModel,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.PatchRequest(options, newModel, oldModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("New");
		result.Value.ShouldBe(20m);
	}

	[Fact]
	public async Task PatchRequest_ShouldReturnOriginalModel_WhenNoChangesDetected()
	{
		// Arrange
		TestModel model = new() { Id = 1, Name = "Test", Value = 10m };
		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.PatchRequest(options, model, model, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(model);
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.MustNotHaveHappened();
	}

	[Fact]
	public async Task PatchRequest_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		TestModel oldModel = new() { Id = 1, Name = "Old", Value = 10m };
		TestModel newModel = new() { Id = 1, Name = "New", Value = 20m };
		RestObject<TestModel> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		TestModel? result = await wrapper.PatchRequest(options, newModel, oldModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task PatchRequest_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel oldModel = new() { Id = 1, Name = "Old" };
		TestModel newModel = new() { Id = 1, Name = "New" };
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.PatchRequest(options, newModel, oldModel));
	}

	#endregion

	#region PutRequest Tests

	[Fact]
	public async Task PutRequest_ShouldReturnResult_WhenRequestIsSuccessful()
	{
		// Arrange
		TestModel replacementModel = new() { Id = 1, Name = "Updated", Value = 25m };
		RestObject<TestModel> restObject = new()
		{
			Result = replacementModel,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.PutRequest(options, replacementModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Updated");
		result.Value.ShouldBe(25m);
	}

	[Fact]
	public async Task PutRequest_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		TestModel replacementModel = new() { Id = 1, Name = "Updated" };
		RestObject<TestModel> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		TestModel? result = await wrapper.PutRequest(options, replacementModel, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task PutRequest_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		TestModel replacementModel = new() { Id = 1, Name = "Updated" };
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.PutRequest(options, replacementModel));
	}

	#endregion

	#region DeleteRequest Tests

	[Fact]
	public async Task DeleteRequest_ShouldReturnResult_WhenRequestIsSuccessful()
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

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.DeleteRequest<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Deleted");
	}

	[Fact]
	public async Task DeleteRequest_ShouldReturnNull_WhenRequestFails()
	{
		// Arrange
		RestObject<TestModel> restObject = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 1));

		// Act
		TestModel? result = await wrapper.DeleteRequest<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteRequest_ShouldThrowHttpRequestException_OnException()
	{
		// Arrange
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Throws(new InvalidOperationException("Test exception"));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act & Assert
		await Should.ThrowAsync<HttpRequestException>(async () => await wrapper.DeleteRequest<TestModel>(options));
	}

	#endregion

	#region Helper Methods

	private static async IAsyncEnumerable<T?> AsyncEnumerableFromList<T>(List<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		foreach (T item in items)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Yield();
			yield return item;
		}
	}

	#endregion

	#region Cancellation Tests

	[Fact]
	public async Task Get_ShouldRespectCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, cts.Token);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task PostRequest_ShouldRespectCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		TestModel postObject = new() { Id = 1, Name = "Test" };
		RestObject<TestModel> restObject = new()
		{
			Result = postObject,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi");

		// Act
		TestModel? result = await wrapper.PostRequest(options, postObject, cts.Token);

		// Assert
		result.ShouldNotBeNull();
	}

	#endregion

	#region Retry Logic Tests

	[Fact]
	public async Task Get_ShouldStopRetrying_WhenMaxRetryReached()
	{
		// Arrange
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.Returns(Task.FromResult(failedResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi", ResilienceOptions: new ResilienceOptions(MaxRetry: 3, RetryDelay: 10));

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(3, Times.Exactly);
	}

	[Fact]
	public async Task PostRequest_ShouldRetry_WithExponentialBackoff()
	{
		// Arrange
		RestObject<TestModel> failedResponse = new()
		{
			Result = null,
			Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
		};

		TestModel postObject = new() { Id = 1, Name = "Test" };
		TestModel expectedResult = new() { Id = 1, Name = "Test", Value = 10m };
		RestObject<TestModel> successResponse = new()
		{
			Result = expectedResult,
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.ReturnsNextFromSequence(Task.FromResult(failedResponse), Task.FromResult(successResponse));

		RestHelperOptions options = new("test-endpoint", "TestApi",
			ResilienceOptions: new ResilienceOptions(MaxRetry: 2, RetryDelay: 10, DelayBackoffType: RestHelperConstants.EDelayBackoffType.Exponential, UseJitter: false));

		// Act
		TestModel? result = await wrapper.PostRequest(options, postObject, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
			.MustHaveHappened(2, Times.Exactly);
	}

	#endregion

	#region FillDefaultOptions Tests

	[Fact]
	public async Task FillDefaultOptions_ShouldNotModifyOptions_WhenNoDefaultConfigProvided()
	{
		// Arrange - wrapper has no default config
		JsonSerializerOptions originalJsonOptions = new();
		MessagePackSerializerOptions originalMessagePackOptions = MessagePackSerializerOptions.Standard;
		CompressionOptions originalCompressionOptions = new();

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi",
				JsonSerializerOptions: originalJsonOptions,
				MessagePackSerializerOptions: originalMessagePackOptions,
				CompressionOptions: originalCompressionOptions);

		// Act
		await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - no default config means FillDefaultOptions is a no-op
		options.JsonSerializerOptions.ShouldBeSameAs(originalJsonOptions);
		options.MessagePackSerializerOptions.ShouldBeSameAs(originalMessagePackOptions);
		options.CompressionOptions.ShouldBeSameAs(originalCompressionOptions);
		options.UseBearerToken.ShouldBeFalse();
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldInitializeResilienceOptions_WhenNullOnOptionsAndDefaultConfigProvided()
	{
		// Arrange
		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig());

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // ResilienceOptions is null

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - FillDefaultOptions should initialize ResilienceOptions when a default config is present
		options.ResilienceOptions.ShouldNotBeNull();
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldFillGetBearerTokenFunc_WhenNotSetOnOptions()
	{
		// Arrange
		static ValueTask<string> defaultBearerTokenFunc(string _, bool __) => new("default-token");

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			ResilienceOptions = new ResilienceOptions(GetBearerTokenFunc: defaultBearerTokenFunc)
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // No GetBearerTokenFunc

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - GetBearerTokenFunc was filled from the default config
		options.ResilienceOptions.ShouldNotBeNull();
		options.ResilienceOptions.GetBearerTokenFunc.ShouldNotBeNull();
		string token = await options.ResilienceOptions.GetBearerTokenFunc("TestApi", false);
		token.ShouldBe("default-token");
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldNotOverrideGetBearerTokenFunc_WhenAlreadySetOnOptions()
	{
		// Arrange
		static ValueTask<string> defaultFunc(string _, bool __) => new("default-token");
		static ValueTask<string> optionsFunc(string _, bool __) => new("options-token");

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			ResilienceOptions = new ResilienceOptions(GetBearerTokenFunc: defaultFunc)
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi",
				ResilienceOptions: new ResilienceOptions(GetBearerTokenFunc: optionsFunc));

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - options' GetBearerTokenFunc must not be replaced by the default config's func
		options.ResilienceOptions!.GetBearerTokenFunc.ShouldNotBeNull();
		string token = await options.ResilienceOptions.GetBearerTokenFunc("TestApi", false);
		token.ShouldBe("options-token");
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldOverrideUseBearerToken_WhenDefaultConfigHasValue()
	{
		// Arrange
		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			UseBearerToken = true,
			ResilienceOptions = new ResilienceOptions(GetBearerTokenFunc: (_, __) => new("some-token"))
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // UseBearerToken defaults to false

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - UseBearerToken was overridden to true by the default config
		options.UseBearerToken.ShouldBeTrue();
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(
				A<RequestOptions<TestModel>>.That.Matches(r => r.BearerToken == "some-token"), A<CancellationToken>._))
				.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldNotOverrideUseBearerToken_WhenDefaultConfigHasNullUseBearerToken()
	{
		// Arrange
		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			UseBearerToken = null // null means don't override
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // UseBearerToken = false

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - UseBearerToken not overridden because default config value is null
		options.UseBearerToken.ShouldBeFalse();
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldFillJsonSerializerOptions_WhenNotSetOnOptions()
	{
		// Arrange
		JsonSerializerOptions defaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			JsonSerializerOptions = defaultJsonOptions
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // JsonSerializerOptions is null

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - JsonSerializerOptions was filled from the default config
		options.JsonSerializerOptions.ShouldBeSameAs(defaultJsonOptions);
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldNotOverrideJsonSerializerOptions_WhenAlreadySetOnOptions()
	{
		// Arrange
		JsonSerializerOptions defaultJsonOptions = new() { PropertyNameCaseInsensitive = true };
		JsonSerializerOptions optionsJsonOptions = new() { WriteIndented = true };

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			JsonSerializerOptions = defaultJsonOptions
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", JsonSerializerOptions: optionsJsonOptions);

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - options' JsonSerializerOptions was not overridden by the default config
		options.JsonSerializerOptions.ShouldBeSameAs(optionsJsonOptions);
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldFillMessagePackSerializerOptions_WhenNotSetOnOptions()
	{
		// Arrange
		MessagePackSerializerOptions defaultMessagePackSerializerOptions = MessagePackSerializerOptions.Standard;

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			MessagePackSerializerOptions = defaultMessagePackSerializerOptions
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // MessagePackSerializerOptions is null

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - MessagePackSerializerOptions was filled from the default config
		options.MessagePackSerializerOptions.ShouldBeSameAs(defaultMessagePackSerializerOptions);
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldNotOverrideMessagePackSerializerOptions_WhenAlreadySetOnOptions()
	{
		// Arrange
		MessagePackSerializerOptions defaultMessagePackSerializerOptions = MessagePackSerializerOptions.Standard;
		MessagePackSerializerOptions optionsMessagePackSerializerOptions = MessagePackSerializerOptions.Standard;

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			MessagePackSerializerOptions = defaultMessagePackSerializerOptions
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", MessagePackSerializerOptions: optionsMessagePackSerializerOptions);

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - options' MessagePackSerializerOptions was not overridden by the default config
		options.MessagePackSerializerOptions.ShouldBeSameAs(optionsMessagePackSerializerOptions);
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldFillCompressionOptions_WhenNotSetOnOptions()
	{
		// Arrange
		CompressionOptions defaultCompressionOptions = new(UseCompression: true);

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			CompressionOptions = defaultCompressionOptions
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi"); // CompressionOptions is null

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - CompressionOptions was filled from the default config
		options.CompressionOptions.ShouldBeSameAs(defaultCompressionOptions);
	}

	[Fact]
	public async Task FillDefaultOptions_ShouldNotOverrideCompressionOptions_WhenAlreadySetOnOptions()
	{
		// Arrange
		CompressionOptions defaultCompressionOptions = new(UseCompression: true);
		CompressionOptions optionsCompressionOptions = new(UseCompression: false);

		IRestClientFactory localFactory = A.Fake<IRestClientFactory>();
		IRestClient localClient = A.Fake<IRestClient>();
		A.CallTo(() => localFactory.CreateClient(A<string>._)).Returns(localClient);
		A.CallTo(() => localClient.BaseAddress).Returns(new Uri("http://test.com/"));
		RestHelpersWrapper wrapperWithDefaults = new(localFactory, new RestHelperOptionsDefaultConfig
		{
			CompressionOptions = defaultCompressionOptions
		});

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};
		A.CallTo(() => localClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._))
				.Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", CompressionOptions: optionsCompressionOptions);

		// Act
		await wrapperWithDefaults.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert - options' CompressionOptions was not overridden by the default config
		options.CompressionOptions.ShouldBeSameAs(optionsCompressionOptions);
	}

	#endregion

	#region Headers and Options Tests

	[RetryFact(3)]
	public async Task Get_ShouldIncludeCustomHeaders_WhenProvided()
	{
		// Arrange
		Dictionary<string, string> customHeaders = new()
			{
				{ "X-Custom-Header", "CustomValue" }
			};

		RestObject<TestModel> restObject = new()
		{
			Result = new TestModel { Id = 1 },
			Response = new HttpResponseMessage(HttpStatusCode.OK)
		};

		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(A<RequestOptions<TestModel>>._, A<CancellationToken>._)).Returns(Task.FromResult(restObject));

		RestHelperOptions options = new("test-endpoint", "TestApi", HttpHeaders: customHeaders);

		// Act
		TestModel? result = await wrapper.Get<TestModel>(options, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		A.CallTo(() => fakeRestClient.RestObjectRequest<TestModel, TestModel>(
			A<RequestOptions<TestModel>>.That.Matches(r => r.HttpHeaders != null && r.HttpHeaders.ContainsKey("X-Custom-Header")),
			A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}

	#endregion
}
