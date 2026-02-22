using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest;

namespace Web.Requests.Tests;

public sealed class RequestOptionsTests
{
	[Fact]
	public void RedactedUrl_ReturnsRedactedUri_WhenUrlIsValid()
	{
		RequestOptions<string> options = new()
		{
			Url = "https://api.example.com/users?token=secret123&key=value"
		};

		string redacted = options.RedactedUrl;

		// Should redact the query parameters
		redacted.ShouldNotContain("secret123");
		redacted.ShouldContain("api.example.com");
	}

	[Fact]
	public void RedactedUrl_ReturnsOriginalUrl_WhenUrlIsNull()
	{
		RequestOptions<string> options = new()
		{
			Url = null!
		};

		string redacted = options.RedactedUrl;

		redacted.ShouldBeNull();
	}

	[Fact]
	public void RedactedUrl_ReturnsOriginalUrl_WhenUrlIsEmpty()
	{
		RequestOptions<string> options = new()
		{
			Url = string.Empty
		};

		string redacted = options.RedactedUrl;

		redacted.ShouldBe(string.Empty);
	}

	[Fact]
	public void RedactedUrl_ReturnsOriginalUrl_WhenUrlIsWhitespace()
	{
		RequestOptions<string> options = new()
		{
			Url = "   "
		};

		string redacted = options.RedactedUrl;

		redacted.ShouldBe("   ");
	}

	[Fact]
	public void RedactedUrl_ReturnsErrorMessage_WhenUrlIsInvalid()
	{
		RequestOptions<string> options = new()
		{
			Url = "not a valid url format $$$ invalid"
		};

		string redacted = options.RedactedUrl;

		redacted.ShouldBe("<Error Redacting URL>");
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
