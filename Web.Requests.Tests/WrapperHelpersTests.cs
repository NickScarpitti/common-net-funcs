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
	#region PopulateHeaders Tests

	[Fact]
	public void PopulateHeaders_ShouldAddJsonHeaders_WhenStreaming()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api");

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: true);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(Json);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldBe("identity");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddCustomHeaders_WhenProvided()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", HttpHeaders: new Dictionary<string, string>
		{
			["X-Custom"] = "custom-value",
			["X-Another"] = "another-value"
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("X-Custom");
		headers["X-Custom"].ShouldBe("custom-value");
		headers.ShouldContainKey("X-Another");
		headers["X-Another"].ShouldBe("another-value");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddGzipMemPackHeaders_WhenConfigured()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip,
			UseMemPack = true
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(MemPack);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("gzip");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddBrotliMemPackHeaders_WhenConfigured()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli,
			UseMemPack = true
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(MemPack);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("br");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddGzipMsgPackHeaders_WhenConfigured()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip,
			UseMsgPack = true
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(MsgPack);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("gzip");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddBrotliMsgPackHeaders_WhenConfigured()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli,
			UseMsgPack = true
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(MsgPack);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("br");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddGzipJsonHeaders_WhenConfigured()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(Json);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("gzip");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddBrotliJsonHeaders_WhenConfigured()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(Json);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("br");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddMemPackHeaders_WhenNoCompressionTypeSpecified()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			UseMemPack = true,
			CompressionType = null
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(MemPack);
		headers.ShouldNotContainKey("Accept-Encoding");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddMsgPackHeaders_WhenNoCompressionTypeSpecified()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			UseMsgPack = true,
			CompressionType = null
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(MsgPack);
		headers.ShouldNotContainKey("Accept-Encoding");
	}

	[Fact]
	public void PopulateHeaders_ShouldAddJsonHeaders_WhenNoCompressionTypeSpecified()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = null
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
		headers.ShouldContainKey(AcceptHeader);
		headers[AcceptHeader].ShouldBe(Json);
		headers.ShouldNotContainKey("Accept-Encoding");
	}

	[Fact]
	public void PopulateHeaders_ShouldDefaultToGzipMemPack_WhenUnknownCompressionType()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			UseMemPack = true,
			CompressionType = (ECompressionType)999 // Invalid type
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MemPack);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("gzip");
	}

	[Fact]
	public void PopulateHeaders_ShouldDefaultToGzipMsgPack_WhenUnknownCompressionType()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			UseMsgPack = true,
			CompressionType = (ECompressionType)999 // Invalid type
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(MsgPack);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("gzip");
	}

	[Fact]
	public void PopulateHeaders_ShouldDefaultToGzipJson_WhenUnknownCompressionType()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = (ECompressionType)999 // Invalid type
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
		headers.ShouldContainKey("Accept-Encoding");
		headers["Accept-Encoding"].ShouldContain("gzip");
	}

	[Fact]
	public void PopulateHeaders_ShouldNotOverrideExistingHeaders()
	{
		Dictionary<string, string> headers = new()
		{
			["Content-Type"] = "custom-type"
		};
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = true,
			CompressionType = ECompressionType.Gzip
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers["Content-Type"].ShouldBe("custom-type");
	}

	[Fact]
	public void PopulateHeaders_ShouldCombineCustomAndCompressionHeaders()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api",
			HttpHeaders: new Dictionary<string, string> { ["X-Custom"] = "value" },
			CompressionOptions: new CompressionOptions
			{
				UseCompression = true,
				CompressionType = ECompressionType.Gzip
			});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldContainKey("X-Custom");
		headers["X-Custom"].ShouldBe("value");
		headers.ShouldContainKey("Content-Type");
		headers["Content-Type"].ShouldBe(Json);
		headers.ShouldContainKey("Accept-Encoding");
	}

	[Fact]
	public void PopulateHeaders_ShouldHandleEmptyOptions()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api");

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		// Should have no headers added when no compression or custom headers
		headers.ShouldBeEmpty();
	}

	[Fact]
	public void PopulateHeaders_ShouldNotAddCompressionHeaders_WhenUseCompressionIsFalse()
	{
		Dictionary<string, string> headers = [];
		RestHelperOptions options = new("test", "api", CompressionOptions: new CompressionOptions
		{
			UseCompression = false,
			CompressionType = ECompressionType.Gzip
		});

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers.ShouldBeEmpty();
	}

	[Fact]
	public void PopulateHeaders_ShouldReusePooledDictionary()
	{
		Dictionary<string, string> headers = new()
		{
			["Old-Header"] = "old-value"
		};
		RestHelperOptions options = new("test", "api",
			HttpHeaders: new Dictionary<string, string> { ["New-Header"] = "new-value" });

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		// Should add new headers without removing old ones (caller's responsibility to clear)
		headers.ShouldContainKey("Old-Header");
		headers.ShouldContainKey("New-Header");
	}

	[Fact]
	public void PopulateHeaders_ShouldOverrideExistingCustomHeaders_WhenSameKeyProvided()
	{
		Dictionary<string, string> headers = new()
		{
			["X-Custom"] = "old-value"
		};
		RestHelperOptions options = new("test", "api",
			HttpHeaders: new Dictionary<string, string> { ["X-Custom"] = "new-value" });

		WrapperHelpers.PopulateHeaders(headers, options, isStreaming: false);

		headers["X-Custom"].ShouldBe("new-value");
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
			GetBearerTokenFunc = (_, __) =>
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
			GetBearerTokenFunc = (_, refresh) =>
			{
				if (refresh)
				{
					refreshCalled = true;
				}

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
			GetBearerTokenFunc = (_, refresh) =>
			{
				if (refresh)
				{
					refreshCalled = true;
				}


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
			ShouldRetryFunc = (_, __) => true
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
		const string bearerToken = "test-token";

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

	#region GetRequestOptions URL Construction Tests

	[Theory]
	[InlineData("/v1/endpoint", "http://api.test.com/", "http://api.test.com/v1/endpoint")]                              // Base trailing slash, relative leading slash
	[InlineData("v1/endpoint", "http://api.test.com/", "http://api.test.com/v1/endpoint")]                               // Base trailing slash, relative no leading slash
	[InlineData("/v1/endpoint", "http://api.test.com", "http://api.test.com/v1/endpoint")]                               // Base no trailing slash, relative leading slash
	[InlineData("v1/endpoint", "http://api.test.com", "http://api.test.com/v1/endpoint")]                                // Base no trailing slash, relative no leading slash
	[InlineData("/v1/endpoint", "http://api.test.com/myapp/", "http://api.test.com/myapp/v1/endpoint")]                  // Base with path prefix (trailing slash)
	[InlineData("v1/endpoint", "http://api.test.com/myapp", "http://api.test.com/myapp/v1/endpoint")]                    // Base with path prefix (no trailing slash)
	[InlineData("///v1/endpoint", "http://api.test.com/", "http://api.test.com/v1/endpoint")]                            // Multiple leading slashes on relative
	[InlineData("v1/endpoint", null, "v1/endpoint")]                                                                     // Null base, no leading slash on relative
	[InlineData("/v1/endpoint", null, "/v1/endpoint")]                                                                   // Null base, leading slash preserved on relative
	[InlineData("v1/endpoint", "http://api.test.com:8080/", "http://api.test.com:8080/v1/endpoint")]                     // Base with port number
	[InlineData("v1/endpoint", "https://secure.api.com/", "https://secure.api.com/v1/endpoint")]                         // HTTPS scheme
	[InlineData("v1/resources/items/details", "http://api.test.com/", "http://api.test.com/v1/resources/items/details")] // Deep nested relative path
	[InlineData("v1/endpoint?foo=bar&baz=1", "http://api.test.com/", "http://api.test.com/v1/endpoint?foo=bar&baz=1")]   // Query string on relative
	[InlineData("endpoint", "http://api.test.com/app/v2/", "http://api.test.com/app/v2/endpoint")]                       // Deep base path prefix
	[InlineData("items", "http://localhost/", "http://localhost/items")]                                                 // Simple base and relative (common happy path)
	[InlineData("v1/my.endpoint", "http://api.test.com/", "http://api.test.com/v1/my.endpoint")]                         // Dot in path segment
	public void GetRequestOptions_Url_CombinesBaseAndRelativeCorrectly(string optionsUrl, string? baseAddressString, string expectedUrl)
	{
		RestHelperOptions options = new(optionsUrl, "api");
		Uri? baseAddress = baseAddressString != null ? new Uri(baseAddressString) : null;

		RequestOptions<string> result = WrapperHelpers.GetRequestOptions<string>(options, baseAddress, new Dictionary<string, string>(), HttpMethod.Get, null);

		result.Url.ShouldBe(expectedUrl);
	}

	// Empty Url is rejected by RestHelperOptions constructor
	[Fact]
	public void GetRequestOptions_Url_EmptyRelativePath_ThrowsOnConstruction()
	{
		Should.Throw<ArgumentException>(() => new RestHelperOptions("", "api"));
	}

	#endregion
}
