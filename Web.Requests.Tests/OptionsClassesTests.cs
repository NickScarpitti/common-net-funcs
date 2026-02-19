using System.Net;
using System.Text.Json;
using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest.Options;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;

namespace Web.Requests.Tests;

public sealed class OptionsClassesTests
{
	#region CompressionOptions Tests

	[Fact]
	public void CompressionOptions_DefaultConstructor_SetsDefaults()
	{
		// Arrange & Act

		CompressionOptions options = new();

		// Assert

		options.UseCompression.ShouldBeFalse();
		options.CompressionType.ShouldBeNull();
		options.UseMemPack.ShouldBeFalse();
		options.UseMsgPack.ShouldBeFalse();
	}

	[Fact]
	public void CompressionOptions_ParameterizedConstructor_SetsValues()
	{
		// Arrange & Act

		CompressionOptions options = new(UseCompression: true, CompressionType: ECompressionType.Gzip, UseMemPack: true, UseMsgPack: false);

		// Assert

		options.UseCompression.ShouldBeTrue();
		options.CompressionType.ShouldBe(ECompressionType.Gzip);
		options.UseMemPack.ShouldBeTrue();
		options.UseMsgPack.ShouldBeFalse();
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Deflate)]
	public void CompressionOptions_SupportsAllCompressionTypes(ECompressionType compressionType)
	{
		// Arrange & Act

		CompressionOptions options = new(CompressionType: compressionType);

		// Assert

		options.CompressionType.ShouldBe(compressionType);
	}

	[Fact]
	public void CompressionOptions_PropertiesCanBeModified()
	{
		// Arrange & Act

		CompressionOptions options = new()
		{
			UseCompression = true,
			CompressionType = ECompressionType.Brotli,
			UseMemPack = true,
			UseMsgPack = true
		};

		// Assert

		options.UseCompression.ShouldBeTrue();
		options.CompressionType.ShouldBe(ECompressionType.Brotli);
		options.UseMemPack.ShouldBeTrue();
		options.UseMsgPack.ShouldBeTrue();
	}

	#endregion

	#region ResilienceOptions Tests


	[Fact]
	public void ResilienceOptions_DefaultConstructor_SetsDefaults()
	{
		// Arrange & Act

		ResilienceOptions options = new();

		// Assert

		options.MaxRetry.ShouldBe(10);
		options.RetryDelay.ShouldBe(TimeSpan.FromMilliseconds(1000));
		options.TimeoutValue.ShouldBe(TimeSpan.FromSeconds(100));
		options.RunOnce.ShouldBeFalse();
		options.NullOk.ShouldBeFalse();
		options.DelayBackoffType.ShouldBe(EDelayBackoffType.Constant);
		options.UseJitter.ShouldBeTrue();
		options.ShouldRetryByStatusFunc.ShouldBeNull();
		options.ShouldRetryFunc.ShouldBeNull();
		options.GetBearerTokenFunc.ShouldBeNull();
	}

	[Fact]
	public void ResilienceOptions_ParameterizedConstructor_SetsCustomValues()
	{
		// Arrange

		static bool customRetryFunc(HttpStatusCode status) => status == HttpStatusCode.BadRequest;

		// Act

		ResilienceOptions options = new(
			MaxRetry: 5,
			RetryDelay: 500,
			TimeoutValue: 30,
			RunOnce: true,
			NullOk: true,
			ShouldRetryByStatusFunc: customRetryFunc,
			DelayBackoffType: EDelayBackoffType.Exponential,
			UseJitter: false
		);

		// Assert

		options.MaxRetry.ShouldBe(5);
		options.RetryDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
		options.TimeoutValue.ShouldBe(TimeSpan.FromSeconds(30));
		options.RunOnce.ShouldBeTrue();
		options.NullOk.ShouldBeTrue();
		options.ShouldRetryByStatusFunc.ShouldBe(customRetryFunc);
		options.DelayBackoffType.ShouldBe(EDelayBackoffType.Exponential);
		options.UseJitter.ShouldBeFalse();
	}

	[Fact]
	public void ResilienceOptions_TimeoutValue_HandlesNegativeValue()
	{
		// Arrange & Act

		ResilienceOptions options = new(TimeoutValue: -10);

		// Assert

		options.TimeoutValue.ShouldBe(TimeSpan.FromSeconds(100)); // Should default to 100
	}

	[Fact]
	public void ResilienceOptions_TimeoutValue_HandlesNullValue()
	{
		// Arrange & Act

		ResilienceOptions options = new(TimeoutValue: null);

		// Assert

		options.TimeoutValue.ShouldBe(TimeSpan.FromSeconds(100)); // Should default to 100
	}

	[Fact]
	public void ResilienceOptions_TimeoutValue_AcceptsZero()
	{
		// Arrange & Act

		ResilienceOptions options = new(TimeoutValue: 0);

		// Assert

		options.TimeoutValue.ShouldBe(TimeSpan.Zero); // Zero is accepted as-is
	}

	[Fact]
	public void ResilienceOptions_TimeoutValue_AcceptsPositiveValue()
	{
		// Arrange & Act

		ResilienceOptions options = new(TimeoutValue: 60);

		// Assert

		options.TimeoutValue.ShouldBe(TimeSpan.FromSeconds(60));
	}

	[Fact]
	public void ResilienceOptions_PropertiesCanBeModified()
	{
		// Arrange

		ResilienceOptions options = new();
		static bool newRetryFunc(HttpStatusCode status) => status == HttpStatusCode.ServiceUnavailable;

		// Act

		options.MaxRetry = 3;
		options.RetryDelay = TimeSpan.FromSeconds(2);
		options.TimeoutValue = TimeSpan.FromSeconds(45);
		options.RunOnce = true;
		options.NullOk = true;
		options.ShouldRetryByStatusFunc = newRetryFunc;
		options.DelayBackoffType = EDelayBackoffType.Linear;
		options.UseJitter = false;
		options.RefreshToken = true;

		// Assert

		options.MaxRetry.ShouldBe(3);
		options.RetryDelay.ShouldBe(TimeSpan.FromSeconds(2));
		options.TimeoutValue.ShouldBe(TimeSpan.FromSeconds(45));
		options.RunOnce.ShouldBeTrue();
		options.NullOk.ShouldBeTrue();
		options.ShouldRetryByStatusFunc.ShouldBe(newRetryFunc);
		options.DelayBackoffType.ShouldBe(EDelayBackoffType.Linear);
		options.UseJitter.ShouldBeFalse();
		options.RefreshToken.ShouldBeTrue();
	}

	[Fact]
	public void ResilienceOptions_ShouldRetryFunc_CanBeSet()
	{
		// Arrange

		static bool customRetryFunc(HttpResponseMessage? response, ResilienceOptions opts) => true;

		// Act

		ResilienceOptions options = new(ShouldRetryFunc: customRetryFunc);

		// Assert

		options.ShouldRetryFunc.ShouldBe(customRetryFunc);
		options.ShouldRetryFunc!(null, options).ShouldBeTrue();
	}

	[Fact]
	public async Task ResilienceOptions_GetBearerTokenFunc_CanBeSet()
	{
		// Arrange

		static ValueTask<string> tokenFunc(string apiName, bool forceRefresh) => new($"token-{apiName}-{forceRefresh}");

		// Act

		ResilienceOptions options = new(GetBearerTokenFunc: tokenFunc);

		// Assert

		options.GetBearerTokenFunc.ShouldNotBeNull();
		string token = await options.GetBearerTokenFunc!("TestApi", false);
		token.ShouldBe("token-TestApi-False");
	}

	[Theory]
	[InlineData(EDelayBackoffType.Constant)]
	[InlineData(EDelayBackoffType.Linear)]
	[InlineData(EDelayBackoffType.Exponential)]
	public void ResilienceOptions_SupportsAllBackoffTypes(EDelayBackoffType backoffType)
	{
		// Arrange & Act

		ResilienceOptions options = new(DelayBackoffType: backoffType);

		// Assert

		options.DelayBackoffType.ShouldBe(backoffType);
	}

	#endregion

	#region RestHelperOptions Tests


	[Fact]
	public void RestHelperOptions_ValidConstructor_CreatesInstance()
	{
		// Arrange & Act

		RestHelperOptions options = new(Url: "/api/test", ApiName: "TestApi");

		// Assert

		options.Url.ShouldBe("/api/test");
		options.ApiName.ShouldBe("TestApi");
		options.HttpHeaders.ShouldBeNull();
		options.UseBearerToken.ShouldBeFalse();
		options.BearerToken.ShouldBeNull();
		options.UseNewtonsoftDeserializer.ShouldBeFalse();
		options.LogQuery.ShouldBeTrue();
		options.LogBody.ShouldBeTrue();
		options.CompressionOptions.ShouldBeNull();
		options.MsgPackOptions.ShouldBeNull();
		options.JsonSerializerOptions.ShouldBeNull();
		options.ResilienceOptions.ShouldBeNull();
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenApiNameIsNull()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "/api/test", ApiName: null!));

		ex.ParamName.ShouldBe("ApiName");
		ex.Message.ShouldContain("ApiName cannot be null or whitespace");
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenApiNameIsEmpty()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "/api/test", ApiName: ""));

		ex.ParamName.ShouldBe("ApiName");
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenApiNameIsWhitespace()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "/api/test", ApiName: "   "));

		ex.ParamName.ShouldBe("ApiName");
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenUrlIsNull()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: null!, ApiName: "TestApi"));

		ex.ParamName.ShouldBe("Url");
		ex.Message.ShouldContain("Url cannot be null or whitespace");
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenUrlIsEmpty()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "", ApiName: "TestApi"));

		ex.ParamName.ShouldBe("Url");
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenUrlIsWhitespace()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "   ", ApiName: "TestApi"));

		ex.ParamName.ShouldBe("Url");
	}

	[Fact]
	public void RestHelperOptions_Constructor_ThrowsWhenBearerTokenRequiredButMissing()
	{
		// Arrange & Act & Assert

		ArgumentException ex = Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "/api/test", ApiName: "TestApi", UseBearerToken: true, BearerToken: null));

		ex.ParamName.ShouldBe("BearerToken");
		ex.Message.ShouldContain("BearerToken cannot be null or whitespace when UseBearerToken is true");
	}

	[Fact]
	public void RestHelperOptions_Constructor_AllowsNullBearerTokenWhenNotUsed()
	{
		// Arrange & Act

		RestHelperOptions options = new(Url: "/api/test", ApiName: "TestApi", UseBearerToken: false, BearerToken: null);

		// Assert

		options.UseBearerToken.ShouldBeFalse();
		options.BearerToken.ShouldBeNull();
	}

	[Fact]
	public void RestHelperOptions_Constructor_AllowsNullBearerTokenWhenGetBearerTokenFuncProvided()
	{
		// Arrange

		ResilienceOptions resilienceOptions = new(GetBearerTokenFunc: (apiName, forceRefresh) => new ValueTask<string>("dynamic-token"));

		// Act

		RestHelperOptions options = new(Url: "/api/test", ApiName: "TestApi", UseBearerToken: true, BearerToken: null, ResilienceOptions: resilienceOptions);

		// Assert

		options.UseBearerToken.ShouldBeTrue();
		options.BearerToken.ShouldBeNull();
		options.ResilienceOptions?.GetBearerTokenFunc.ShouldNotBeNull();
	}

	[Fact]
	public void RestHelperOptions_Constructor_SetsAllProperties()
	{
		// Arrange

		Dictionary<string, string> headers = new() { { "X-Custom", "Value" } };
		CompressionOptions compressionOptions = new(UseCompression: true);
		MsgPackOptions msgPackOptions = new() { UseMsgPackCompression = true };
		JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
		ResilienceOptions resilienceOptions = new(MaxRetry: 3);

		// Act

		RestHelperOptions options = new(
			Url: "/api/endpoint",
			ApiName: "ProductionApi",
			HttpHeaders: headers,
			UseBearerToken: true,
			BearerToken: "test-token",
			UseNewtonsoftDeserializer: true,
			LogQuery: false,
			LogBody: false,
			CompressionOptions: compressionOptions,
			MsgPackOptions: msgPackOptions,
			JsonSerializerOptions: jsonOptions,
			ResilienceOptions: resilienceOptions
		);

		// Assert

		options.Url.ShouldBe("/api/endpoint");
		options.ApiName.ShouldBe("ProductionApi");
		options.HttpHeaders.ShouldBe(headers);
		options.UseBearerToken.ShouldBeTrue();
		options.BearerToken.ShouldBe("test-token");
		options.UseNewtonsoftDeserializer.ShouldBeTrue();
		options.LogQuery.ShouldBeFalse();
		options.LogBody.ShouldBeFalse();
		options.CompressionOptions.ShouldBe(compressionOptions);
		options.MsgPackOptions.ShouldBe(msgPackOptions);
		options.JsonSerializerOptions.ShouldBe(jsonOptions);
		options.ResilienceOptions.ShouldBe(resilienceOptions);
	}

	[Fact]
	public void RestHelperOptions_PropertiesCanBeModified()
	{
		// Arrange

		RestHelperOptions options = new("/api/test", "TestApi")
		{
			// Act

			Url = "/api/updated",
			ApiName = "UpdatedApi",
			HttpHeaders = new Dictionary<string, string> { { "New-Header", "NewValue" } },
			UseBearerToken = true,
			BearerToken = "new-token",
			UseNewtonsoftDeserializer = true,
			LogQuery = false,
			LogBody = false,
			CompressionOptions = new CompressionOptions(UseCompression: true),
			MsgPackOptions = new MsgPackOptions { UseMsgPackCompression = true },
			JsonSerializerOptions = new JsonSerializerOptions(),
			ResilienceOptions = new ResilienceOptions()
		};

		// Assert

		options.Url.ShouldBe("/api/updated");
		options.ApiName.ShouldBe("UpdatedApi");
		options.HttpHeaders.ShouldNotBeNull();
		options.UseBearerToken.ShouldBeTrue();
		options.BearerToken.ShouldBe("new-token");
		options.UseNewtonsoftDeserializer.ShouldBeTrue();
		options.LogQuery.ShouldBeFalse();
		options.LogBody.ShouldBeFalse();
		options.CompressionOptions.ShouldNotBeNull();
		options.MsgPackOptions.ShouldNotBeNull();
		options.JsonSerializerOptions.ShouldNotBeNull();
		options.ResilienceOptions.ShouldNotBeNull();
	}

	[Fact]
	public void RestHelperOptions_Constructor_AcceptsEmptyBearerToken()
	{
		// Arrange & Act & Assert
		Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "/api/test", ApiName: "TestApi", UseBearerToken: true, BearerToken: ""));
	}

	[Fact]
	public void RestHelperOptions_Constructor_AcceptsWhitespaceBearerToken()
	{
		// Arrange & Act & Assert
		Should.Throw<ArgumentException>(() => new RestHelperOptions(Url: "/api/test", ApiName: "TestApi", UseBearerToken: true, BearerToken: "   "));
	}

	#endregion
}
