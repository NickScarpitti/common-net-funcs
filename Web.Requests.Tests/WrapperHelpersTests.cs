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

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnGzipMemPackHeaders_WhenConfigured()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip,
			UseMemPack = true
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
		headers.ShouldContainKey("Accept-Encoding");
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnBrotliMemPackHeaders_WhenConfigured()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli,
			UseMemPack = true
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnGzipMsgPackHeaders_WhenConfigured()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip,
			UseMsgPack = true
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnBrotliMsgPackHeaders_WhenConfigured()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli,
			UseMsgPack = true
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnGzipJsonHeaders_WhenConfigured()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnBrotliJsonHeaders_WhenConfigured()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnMemPackHeaders_WhenNoCompressionTypeSpecified()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			UseMemPack = true,
			CompressionType = null
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnMsgPackHeaders_WhenNoCompressionTypeSpecified()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			UseMsgPack = true,
			CompressionType = null
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldReturnJsonHeaders_WhenNoCompressionTypeSpecified()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = null
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldDefaultToGzipMemPack_WhenUnknownCompressionType()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			UseMemPack = true,
			CompressionType = (ECompressionType)999 // Invalid type
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldDefaultToGzipMsgPack_WhenUnknownCompressionType()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			UseMsgPack = true,
			CompressionType = (ECompressionType)999 // Invalid type
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
	}

	[Fact]
	public void SetCompressionHttpHeaders_ShouldDefaultToGzipJson_WhenUnknownCompressionType()
	{
		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = (ECompressionType)999 // Invalid type
		};

		Dictionary<string, string> headers = WrapperHelpers.SetCompressionHttpHeaders(null, options);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
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

	[Fact]
	public void GetWaitTime_ShouldReturnConstantDelay_WithConstantBackoff()
	{
		ResilienceOptions options = new()
		{
			DelayBackoffType = EDelayBackoffType.Constant,
			RetryDelay = TimeSpan.FromMilliseconds(100),
			UseJitter = false
		};

		TimeSpan waitTime = WrapperHelpers.GetWaitTime(options, attempts: 3);

		waitTime.TotalMilliseconds.ShouldBe(100, tolerance: 0.1);
	}

	[Fact]
	public void GetWaitTime_ShouldReturnLinearDelay_WithLinearBackoff()
	{
		ResilienceOptions options = new()
		{
			DelayBackoffType = EDelayBackoffType.Linear,
			RetryDelay = TimeSpan.FromMilliseconds(100),
			UseJitter = false
		};

		TimeSpan waitTime = WrapperHelpers.GetWaitTime(options, attempts: 3);

		waitTime.TotalMilliseconds.ShouldBe(300, tolerance: 0.1);
	}

	[Fact]
	public void GetWaitTime_ShouldReturnExponentialDelay_WithExponentialBackoff()
	{
		ResilienceOptions options = new()
		{
			DelayBackoffType = EDelayBackoffType.Exponential,
			RetryDelay = TimeSpan.FromMilliseconds(10),
			UseJitter = false
		};

		TimeSpan waitTime = WrapperHelpers.GetWaitTime(options, attempts: 3);

		waitTime.TotalMilliseconds.ShouldBe(1000, tolerance: 0.1); // 10^3
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

	[Fact]
	public async Task PopulateBearerToken_ShouldRefreshToken_OnUnauthorized()
	{
		bool refreshCalled = false;
		RestHelperOptions options = new("test", "api", ResilienceOptions: new ResilienceOptions
		{
			GetBearerTokenFunc = (apiName, refresh) =>
			{
				if (refresh) refreshCalled = true;
				return ValueTask.FromResult("refreshed-token");
			}
		});

		HttpResponseMessage unauthorizedResponse = new(HttpStatusCode.Unauthorized);

		string? token = await WrapperHelpers.PopulateBearerToken(options, attempts: 1, unauthorizedResponse, "old-token");

		refreshCalled.ShouldBeTrue();
		token.ShouldBe("refreshed-token");
	}

	[Fact]
	public async Task PopulateBearerToken_ShouldRefreshToken_OnForbidden()
	{
		bool refreshCalled = false;
		RestHelperOptions options = new("test", "api", ResilienceOptions: new ResilienceOptions
		{
			GetBearerTokenFunc = (apiName, refresh) =>
			{
				if (refresh) refreshCalled = true;
				return ValueTask.FromResult("refreshed-token");
			}
		});

		HttpResponseMessage forbiddenResponse = new(HttpStatusCode.Forbidden);

		string? token = await WrapperHelpers.PopulateBearerToken(options, attempts: 1, forbiddenResponse, "old-token");

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

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForUnauthorized()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.Unauthorized);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForForbidden()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.Forbidden);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForInternalServerError()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.InternalServerError);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForBadGateway()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.BadGateway);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForServiceUnavailable()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForGatewayTimeout()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.GatewayTimeout);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForRequestTimeout()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.RequestTimeout);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnTrue_ForTooManyRequests()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeTrue();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_ForBadRequest()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.BadRequest);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeFalse();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_ForNotFound()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.NotFound);

		bool shouldRetry = WrapperHelpers.ShouldRetry(response, options);

		shouldRetry.ShouldBeFalse();
	}

	[Fact]
	public void ShouldRetry_ShouldReturnFalse_ForConflict()
	{
		ResilienceOptions options = new();

		HttpResponseMessage response = new(HttpStatusCode.Conflict);

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

	[Fact]
	public void GetRequestOptions_ShouldSetBodyObject_ForPostRequest()
	{
		RestHelperOptions options = new("test", "api");
		Dictionary<string, string> headers = [];

		RequestOptions<string> result = WrapperHelpers.GetRequestOptions(
			options, new Uri("http://test.com/"), headers, HttpMethod.Post, null, "body-content");

		result.BodyObject.ShouldBe("body-content");
	}

	[Fact]
	public void GetRequestOptions_ShouldSetBodyObject_ForPutRequest()
	{
		RestHelperOptions options = new("test", "api");
		Dictionary<string, string> headers = [];

		RequestOptions<string> result = WrapperHelpers.GetRequestOptions(
			options, new Uri("http://test.com/"), headers, HttpMethod.Put, null, "body-content");

		result.BodyObject.ShouldBe("body-content");
	}

	[Fact]
	public void GetRequestOptions_ShouldSetPatchDocument_ForPatchRequest()
	{
		RestHelperOptions options = new("test", "api");
		Dictionary<string, string> headers = [];
		HttpContent patchContent = new StringContent("{}");

		RequestOptions<string> result = WrapperHelpers.GetRequestOptions<string>(
			options, new Uri("http://test.com/"), headers, HttpMethod.Patch, null, null, patchContent);

		result.PatchDocument.ShouldBe(patchContent);
	}

	[Fact]
	public void GetRequestOptions_ShouldNotSetBodyObject_ForGetRequest()
	{
		RestHelperOptions options = new("test", "api");
		Dictionary<string, string> headers = [];

		RequestOptions<string> result = WrapperHelpers.GetRequestOptions(
			options, new Uri("http://test.com/"), headers, HttpMethod.Get, null, "body-content");

		result.BodyObject.ShouldBeNull();
	}

	#endregion
}
