using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest;

namespace Web.Requests.Tests;

public enum UrlScenario
{
	Valid,
	Null,
	Empty,
	Whitespace,
	Invalid
}

public sealed class RequestOptionsTests
{
	[Theory]
	[InlineData(UrlScenario.Valid)]
	[InlineData(UrlScenario.Null)]
	[InlineData(UrlScenario.Empty)]
	[InlineData(UrlScenario.Whitespace)]
	[InlineData(UrlScenario.Invalid)]
	public void RedactedUrl_HandlesScenarios(UrlScenario scenario)
	{
		// Arrange
		string? url;
		string? expectedResult;

		switch (scenario)
		{
			case UrlScenario.Valid:
				url = "https://api.example.com/users?token=secret123&key=value";
				expectedResult = null; // Will check contains instead
				break;
			case UrlScenario.Null:
				url = null!;
				expectedResult = null;
				break;
			case UrlScenario.Empty:
				url = string.Empty;
				expectedResult = string.Empty;
				break;
			case UrlScenario.Whitespace:
				url = "   ";
				expectedResult = "   ";
				break;
			case UrlScenario.Invalid:
				url = "not a valid url format $$$ invalid";
				expectedResult = "<Error Redacting URL>";
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(scenario));
		}

		RequestOptions<string> options = new() { Url = url };

		// Act
		string redacted = options.RedactedUrl;

		// Assert
		switch (scenario)
		{
			case UrlScenario.Valid:
				redacted.ShouldNotContain("secret123");
				redacted.ShouldContain("api.example.com");
				break;
			case UrlScenario.Null:
				redacted.ShouldBeNull();
				break;
			default:
				redacted.ShouldBe(expectedResult);
				break;
		}
	}

	[Fact]
	public void RequestOptions_DefaultValues_AreSetCorrectly()
	{
		RequestOptions<string> options = new();

		options.HttpMethod.ShouldBe(HttpMethod.Get);
		options.BearerToken.ShouldBeNull();
		options.Timeout.ShouldBeNull();
		options.HttpHeaders.ShouldBeNull();
		options.JsonSerializerOptions.ShouldBeNull();
		options.UseNewtonsoftDeserializer.ShouldBeFalse();
		options.ExpectTaskCancellation.ShouldBeFalse();
		options.LogRequest.ShouldBeFalse();
		options.LogQuery.ShouldBeFalse();
		options.LogBody.ShouldBeFalse();
		options.LogResponse.ShouldBeFalse();
		options.MsgPackOptions.ShouldBeNull();
		options.PatchDocument.ShouldBeNull();
		options.BodyObject.ShouldBeNull();
	}

	[Fact]
	public void RequestOptions_CanSetAllProperties()
	{
		Dictionary<string, string> headers = new() { ["X-Custom"] = "value" };
		System.Text.Json.JsonSerializerOptions jsonOptions = new();
		MsgPackOptions msgPackOptions = new();
		HttpContent patchContent = new StringContent("{}");

		RequestOptions<string> options = new()
		{
			Url = "https://test.com",
			HttpMethod = HttpMethod.Post,
			BearerToken = "token123",
			Timeout = 30,
			HttpHeaders = headers,
			JsonSerializerOptions = jsonOptions,
			UseNewtonsoftDeserializer = true,
			ExpectTaskCancellation = true,
			LogRequest = true,
			LogQuery = true,
			LogBody = true,
			LogResponse = true,
			MsgPackOptions = msgPackOptions,
			PatchDocument = patchContent,
			BodyObject = "test-body"
		};

		options.Url.ShouldBe("https://test.com");
		options.HttpMethod.ShouldBe(HttpMethod.Post);
		options.BearerToken.ShouldBe("token123");
		options.Timeout.ShouldBe(30);
		options.HttpHeaders.ShouldBe(headers);
		options.JsonSerializerOptions.ShouldBe(jsonOptions);
		options.UseNewtonsoftDeserializer.ShouldBeTrue();
		options.ExpectTaskCancellation.ShouldBeTrue();
		options.LogRequest.ShouldBeTrue();
		options.LogQuery.ShouldBeTrue();
		options.LogBody.ShouldBeTrue();
		options.LogResponse.ShouldBeTrue();
		options.MsgPackOptions.ShouldBe(msgPackOptions);
		options.PatchDocument.ShouldBe(patchContent);
		options.BodyObject.ShouldBe("test-body");
	}
}
