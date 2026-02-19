using System.Net;
using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest;
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;

namespace Web.Requests.Tests;

public sealed class WrapperHelpersTests
{
	public enum ContentTypeScenario
	{
		MemPack,
		MsgPack,
		Json
	}

	public enum CompressionScenario
	{
		Gzip,
		Brotli,
		None,
		Unknown
	}

	#region GetHeaders Tests

	[Fact]
	public void GetHeaders_ShouldReturnJsonHeaders_WhenStreaming()
	{
		RestHelperOptions options = new("test", "api");

		Dictionary<string, string> headers = WrapperHelpers.GetHeaders(options, isStreaming: true);

		headers.ShouldContain(h => h.Key == AcceptHeader && h.Value == Json);
	}

	[Fact]
	public void GetHeaders_ShouldIncludeCustomHeaders_WhenProvided()
	{
		RestHelperOptions options = new("test", "api", HttpHeaders: new Dictionary<string, string>
		{
			["X-Custom"] = "value"
		});

		Dictionary<string, string> headers = WrapperHelpers.GetHeaders(options, isStreaming: false);

		headers.ShouldContainKey("X-Custom");
		headers["X-Custom"].ShouldBe("value");
	}

	#endregion

	#region SetCompressionHttpHeaders Tests

	[Theory]
	[InlineData(CompressionScenario.Gzip, ContentTypeScenario.MemPack, MemPack)]
	[InlineData(CompressionScenario.Gzip, ContentTypeScenario.MsgPack, MsgPack)]
	[InlineData(CompressionScenario.Gzip, ContentTypeScenario.Json, Json)]
	[InlineData(CompressionScenario.Brotli, ContentTypeScenario.MemPack, MemPack)]
	[InlineData(CompressionScenario.Brotli, ContentTypeScenario.MsgPack, MsgPack)]
	[InlineData(CompressionScenario.Brotli, ContentTypeScenario.Json, Json)]
	[InlineData(CompressionScenario.None, ContentTypeScenario.MemPack, MemPack)]
	[InlineData(CompressionScenario.None, ContentTypeScenario.MsgPack, MsgPack)]
	[InlineData(CompressionScenario.None, ContentTypeScenario.Json, Json)]
	[InlineData(CompressionScenario.Unknown, ContentTypeScenario.MemPack, MemPack)]
	[InlineData(CompressionScenario.Unknown, ContentTypeScenario.MsgPack, MsgPack)]
	[InlineData(CompressionScenario.Unknown, ContentTypeScenario.Json, Json)]
	public void SetCompressionHttpHeaders_ShouldReturnCorrectContentType(
		CompressionScenario compressionScenario,
		ContentTypeScenario contentTypeScenario,
		string expectedContentType)
	{
		// Arrange
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = compressionScenario switch
			{
				CompressionScenario.Gzip => ECompressionType.Gzip,
				CompressionScenario.Brotli => ECompressionType.Brotli,
				CompressionScenario.None => null,
				CompressionScenario.Unknown => (ECompressionType)999,
				_ => null
			},
			UseMemPack = contentTypeScenario == ContentTypeScenario.MemPack,
			UseMsgPack = contentTypeScenario == ContentTypeScenario.MsgPack
		};

		// Act
		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		// Assert
		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(expectedContentType);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnJsonNoEncodeHeaders_WhenStreaming()
	{
		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, null, isStreaming: true);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldPreserveExistingHeaders()
	{
		Dictionary<string, string> existingHeaders = new()
		{
			["X-Custom"] = "value"
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(existingHeaders, null);

		headers.ShouldContainKey("X-Custom");
		headers["X-Custom"].ShouldBe("value");
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldNotOverrideExistingHeaders()
	{
		Dictionary<string, string> existingHeaders = new()
		{
			["Content-Type"] = "custom-type"
		};

		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(existingHeaders, options);

		headers["Content-Type"].ShouldBe("custom-type");
	}

	#endregion

	#region GetWaitTime Tests

	[Theory]
	[InlineData(EDelayBackoffType.Constant, 100.0, 3, 100.0)]
	[InlineData(EDelayBackoffType.Linear, 100.0, 3, 300.0)]
	[InlineData(EDelayBackoffType.Exponential, 10.0, 3, 1000.0)]
	public void GetWaitTime_ShouldReturnCorrectDelay_ForBackoffType(
		EDelayBackoffType backoffType,
		double retryDelayMs,
		int attempts,
		double expectedMilliseconds)
	{
		ResilienceOptions options = new()
		{
			DelayBackoffType = backoffType,
			RetryDelay = TimeSpan.FromMilliseconds(retryDelayMs),
			UseJitter = false
		};

		TimeSpan waitTime = WrapperHelpers.GetWaitTime(options, attempts);

		waitTime.TotalMilliseconds.ShouldBe(expectedMilliseconds, tolerance: 0.1);
	}

	[Fact]
	public void GetWaitTime_ShouldAddJitter_WhenUseJitterIsTrue()
	{
		ResilienceOptions options = new()
		{
			DelayBackoffType = EDelayBackoffType.Constant,
			RetryDelay = TimeSpan.FromMilliseconds(100),
			UseJitter = true
		};

		TimeSpan waitTime = WrapperHelpers.GetWaitTime(options, attempts: 1);

		// With jitter, the wait time should be within a reasonable range
		waitTime.TotalMilliseconds.ShouldBeInRange(50, 150);
	}

	[Fact]
	public void GetWaitTime_ShouldDefaultToRetryDelay_WithUnknownBackoffType()
	{
		ResilienceOptions options = new()
		{
			DelayBackoffType = (EDelayBackoffType)999, // Invalid type
			RetryDelay = TimeSpan.FromMilliseconds(200),
			UseJitter = false
		};

		TimeSpan waitTime = WrapperHelpers.GetWaitTime(options, attempts: 1);

		waitTime.TotalMilliseconds.ShouldBe(200, tolerance: 0.1);
	}

	#endregion

	#region PopulateBearerToken Tests

	[Fact]
	public async Task PopulateBearerToken_ShouldReturnProvidedToken_OnFirstAttempt()
	{
		RestHelperOptions options = new("test", "api", BearerToken: "provided-token");

		string? token = await WrapperHelpers.PopulateBearerToken(options, attempts: 0, null, null);

		token.ShouldBe("provided-token");
	}

	[Fact]
	public async Task PopulateBearerToken_ShouldCallGetBearerTokenFunc_WhenNoTokenProvided()
	{
		bool funcCalled = false;
		RestHelperOptions options = new("test", "api", ResilienceOptions: new ResilienceOptions
		{
			GetBearerTokenFunc = (apiName, refresh) =>
			{
				funcCalled = true;
				return ValueTask.FromResult("fetched-token");
			}
		});

		string? token = await WrapperHelpers.PopulateBearerToken(options, attempts: 0, null, null);

		funcCalled.ShouldBeTrue();
		token.ShouldBe("fetched-token");
	}

	[Theory]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.Forbidden)]
	public async Task PopulateBearerToken_ShouldRefreshToken_OnAuthenticationFailure(HttpStatusCode statusCode)
	{
		bool refreshCalled = false;
		RestHelperOptions options = new("test", "api", ResilienceOptions: new ResilienceOptions
		{
			GetBearerTokenFunc = (apiName, refresh) =>
			{
				if (refresh)
				{
					refreshCalled = true;
				}


				return ValueTask.FromResult("refreshed-token");
			}
		});

		HttpResponseMessage response = new(statusCode);

		string? token = await WrapperHelpers.PopulateBearerToken(options, attempts: 1, response, "old-token");

		refreshCalled.ShouldBeTrue();
		token.ShouldBe("refreshed-token");
	}

	[Fact]
	public async Task PopulateBearerToken_ShouldReturnCurrentToken_WhenNoFuncAndNoToken()
	{
		RestHelperOptions options = new("test", "api");

		string? token = await WrapperHelpers.PopulateBearerToken(options, attempts: 1, null, "current-token");

		token.ShouldBe("current-token");
	}

	#endregion

	#region UpdateStreamingHeaders Tests

	[Fact]
	public void UpdateStreamingHeaders_ShouldSetAcceptToJson()
	{
		RestHelperOptions options = new("test", "api");

		WrapperHelpers.UpdateStreamingHeaders(options);

		options.HttpHeaders.ShouldNotBeNull();
		options.HttpHeaders.ShouldContainKey(AcceptHeader);
		options.HttpHeaders[AcceptHeader].ShouldBe(Json);
	}

	[Fact]
	public void UpdateStreamingHeaders_ShouldOverrideExistingAcceptHeader()
	{
		RestHelperOptions options = new("test", "api", HttpHeaders: new Dictionary<string, string>
		{
			[AcceptHeader] = "application/xml"
		});

		WrapperHelpers.UpdateStreamingHeaders(options);

		options.HttpHeaders?[AcceptHeader].ShouldBe(Json);
	}

	#endregion

	#region ShouldRetry Tests

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_WhenCustomFuncReturnsTrue()
	{
		ResilienceOptions options = new()
		{
			ShouldRetryFunc = (response, opts) => true
		};

		HttpResponseMessage response = new(HttpStatusCode.InternalServerError);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeFalse();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_WhenRunOnceAndNotUnauthorized()
	{
		ResilienceOptions options = new() { RunOnce = true };

		HttpResponseMessage response = new(HttpStatusCode.OK);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeFalse();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_WhenResponseIsSuccessful()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.OK);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeFalse();
	}

	[Fact]
	public void ShouldRetry_ShouldUseCustomStatusFunc_WhenProvided()
	{
		bool funcCalled = false;
		ResilienceOptions options = new()
		{
			ShouldRetryByStatusFunc = (statusCode) =>
			{
				funcCalled = true;
				return statusCode == HttpStatusCode.BadRequest;
			}
		};

		HttpResponseMessage response = new(HttpStatusCode.BadRequest);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		funcCalled.ShouldBeTrue();
		shouldRetry.ShouldBeTrue();
	}

	[Theory]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	[InlineData(HttpStatusCode.GatewayTimeout)]
	[InlineData(HttpStatusCode.RequestTimeout)]
	[InlineData(HttpStatusCode.TooManyRequests)]
	public void ShouldRetry_ShouldReturnTrue_ForRetryableStatusCodes(HttpStatusCode statusCode)
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(statusCode);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.Conflict)]
	public void ShouldRetry_ShouldReturnFalse_ForNonRetryableStatusCodes(HttpStatusCode statusCode)
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(statusCode);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeFalse();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_WhenNullResponseAndNullOk()
	{
		ResilienceOptions options = new() { NullOk = true };

		bool shouldRetry = WrapperHelpers.ShouldRetry(null, options);

		shouldRetry.ShouldBeFalse();
	}

	#endregion

	#region GetRequestOptions Tests

	[Fact]
	public void GetRequestOptions_ShouldSetAllProperties()
	{
		RestHelperOptions options = new("test-endpoint", "TestApi")
		{
			ResilienceOptions = new ResilienceOptions { TimeoutValue = TimeSpan.FromSeconds(30), RunOnce = true },
			LogQuery = true,
			LogBody = true,
			JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions(),
			UseNewtonsoftDeserializer = true,
			MsgPackOptions = new MsgPackOptions()
		};

		Dictionary<string, string> headers = new() { ["X-Test"] = "value" };
		Uri baseAddress = new("http://api.test.com/");
		string bearerToken = "test-token";

		RequestOptions<string> result = WrapperHelpers.GetRequestOptions(
			options, baseAddress, headers, HttpMethod.Get, bearerToken, "test-body");

		result.Url.ShouldBe("http://api.test.com/test-endpoint");
		result.HttpMethod.ShouldBe(HttpMethod.Get);
		result.BearerToken.ShouldBe(bearerToken);
		result.Timeout.ShouldBe(30);
		result.HttpHeaders.ShouldBe(headers);
		result.JsonSerializerOptions.ShouldNotBeNull();
		result.UseNewtonsoftDeserializer.ShouldBeTrue();
		result.ExpectTaskCancellation.ShouldBeTrue();
		result.LogQuery.ShouldBeTrue();
		result.LogBody.ShouldBeTrue();
		result.MsgPackOptions.ShouldNotBeNull();
	}

	[Theory]
	[InlineData("Post", true, false)]
	[InlineData("Put", true, false)]
	[InlineData("Patch", false, true)]
	[InlineData("Get", false, false)]
	public void GetRequestOptions_ShouldHandleBodyCorrectly_ForHttpMethod(
		string httpMethod,
		bool shouldSetBodyObject,
		bool shouldSetPatchDocument)
	{
		RestHelperOptions options = new("test", "api");
		Dictionary<string, string> headers = [];
		HttpMethod method = httpMethod switch
		{
			"Post" => HttpMethod.Post,
			"Put" => HttpMethod.Put,
			"Patch" => HttpMethod.Patch,
			"Get" => HttpMethod.Get,
			_ => HttpMethod.Get
		};

		HttpContent? patchContent = shouldSetPatchDocument ? new StringContent("{}") : null;
		string? bodyContent = shouldSetBodyObject || httpMethod == "Get" ? "body-content" : null;

		RequestOptions<string> result = shouldSetPatchDocument
			? WrapperHelpers.GetRequestOptions<string>(options, new Uri("http://test.com/"), headers, method, null, null, patchContent)
			: WrapperHelpers.GetRequestOptions(options, new Uri("http://test.com/"), headers, method, null, bodyContent);

		if (shouldSetBodyObject)
		{
			result.BodyObject.ShouldBe("body-content");
		}
		else if (shouldSetPatchDocument)
		{
			result.PatchDocument.ShouldBe(patchContent);
		}
		else
		{
			result.BodyObject.ShouldBeNull();
		}
	}

	#endregion
}
