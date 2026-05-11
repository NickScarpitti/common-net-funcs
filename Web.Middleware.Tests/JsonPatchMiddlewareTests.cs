using System.Text.Json;
using System.Text.Json.Serialization;
using CommonNetFuncs.Web.Middleware;
using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class JsonPatchRequestMiddlewareTests
{
	[RetryFact(3)]
	public async Task InvokeAsync_WithJsonPatchContentType_NormalizesContentTypeAndCallsNext()
	{
		// Arrange
		RequestDelegate next = A.Fake<RequestDelegate>();
		JsonPatchRequestMiddleware middleware = new(next);
		DefaultHttpContext context = new();
		context.Request.ContentType = "application/json-patch+json";

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Request.ContentType.ShouldBe("application/json; charset=utf-8");
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithUpperCaseJsonPatchContentType_NormalizesContentType()
	{
		// Arrange
		RequestDelegate next = A.Fake<RequestDelegate>();
		JsonPatchRequestMiddleware middleware = new(next);
		DefaultHttpContext context = new();
		context.Request.ContentType = "APPLICATION/JSON-PATCH+JSON";

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Request.ContentType.ShouldBe("application/json; charset=utf-8");
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithNonPatchContentType_LeavesContentTypeUnchanged()
	{
		// Arrange
		RequestDelegate next = A.Fake<RequestDelegate>();
		JsonPatchRequestMiddleware middleware = new(next);
		DefaultHttpContext context = new();
		context.Request.ContentType = "application/json";

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Request.ContentType.ShouldBe("application/json");
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithNullContentType_DoesNotModifyAndCallsNext()
	{
		// Arrange
		RequestDelegate next = A.Fake<RequestDelegate>();
		JsonPatchRequestMiddleware middleware = new(next);
		DefaultHttpContext context = new();
		// No Content-Type header set — ContentType is null

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Request.ContentType.ShouldBeNull();
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}
}

public sealed class JsonPatchDocumentConverterFactoryTests
{
	private readonly JsonPatchDocumentConverterFactory factory = new();

	[RetryFact(3)]
	public void CanConvert_WithJsonPatchDocumentOfT_ReturnsTrue()
	{
		factory.CanConvert(typeof(JsonPatchDocument<JsonPatchTestEntity>)).ShouldBeTrue();
	}

	[RetryFact(3)]
	public void CanConvert_WithNonGenericType_ReturnsFalse()
	{
		factory.CanConvert(typeof(string)).ShouldBeFalse();
	}

	[RetryFact(3)]
	public void CanConvert_WithUnrelatedGenericType_ReturnsFalse()
	{
		factory.CanConvert(typeof(List<string>)).ShouldBeFalse();
	}

	[RetryFact(3)]
	public void CanConvert_WithNonGenericJsonPatchDocument_ReturnsFalse()
	{
		factory.CanConvert(typeof(JsonPatchDocument)).ShouldBeFalse();
	}

	[RetryFact(3)]
	public void CreateConverter_WithJsonPatchDocumentType_ReturnsCorrectConverterType()
	{
		JsonSerializerOptions options = new();

		JsonConverter? converter = factory.CreateConverter(typeof(JsonPatchDocument<JsonPatchTestEntity>), options);

		converter.ShouldNotBeNull();
		converter.ShouldBeOfType<JsonPatchDocumentConverter<JsonPatchTestEntity>>();
	}
}

public sealed class JsonPatchDocumentConverterTests
{
	private readonly JsonSerializerOptions options;

	public JsonPatchDocumentConverterTests()
	{
		options = new JsonSerializerOptions();
		options.Converters.Add(new JsonPatchDocumentConverterFactory());
	}

	[RetryFact(3)]
	public void Read_WithSingleOperation_ReturnsDocumentWithOperation()
	{
		// Arrange
		const string json = """[{"op":"replace","path":"/Name","value":"NewName"}]""";

		// Act
		JsonPatchDocument<JsonPatchTestEntity>? result = JsonSerializer.Deserialize<JsonPatchDocument<JsonPatchTestEntity>>(json, options);

		// Assert
		result.ShouldNotBeNull();
		result.Operations.Count.ShouldBe(1);
		result.Operations[0].op.ShouldBe("replace");
		result.Operations[0].path.ShouldBe("/Name");
	}

	[RetryFact(3)]
	public void Read_WithMultipleOperations_ReturnsAllOperations()
	{
		// Arrange
		const string json = """[{"op":"replace","path":"/Name","value":"NewName"},{"op":"replace","path":"/Age","value":30}]""";

		// Act
		JsonPatchDocument<JsonPatchTestEntity>? result = JsonSerializer.Deserialize<JsonPatchDocument<JsonPatchTestEntity>>(json, options);

		// Assert
		result.ShouldNotBeNull();
		result.Operations.Count.ShouldBe(2);
	}

	[RetryFact(3)]
	public void Read_WithEmptyArray_ReturnsDocumentWithNoOperations()
	{
		// Arrange
		const string json = "[]";

		// Act
		JsonPatchDocument<JsonPatchTestEntity>? result = JsonSerializer.Deserialize<JsonPatchDocument<JsonPatchTestEntity>>(json, options);

		// Assert
		result.ShouldNotBeNull();
		result.Operations.ShouldBeEmpty();
	}

	[RetryFact(3)]
	public void Write_WithPatchDocument_ProducesValidJsonArray()
	{
		// Arrange
		JsonPatchDocument<JsonPatchTestEntity> doc = new();
		doc.Replace(e => e.Name, "NewName");

		// Act
		string json = JsonSerializer.Serialize(doc, options);

		// Assert
		json.ShouldNotBeNullOrEmpty();
		using JsonDocument parsed = JsonDocument.Parse(json);
		parsed.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
		parsed.RootElement.GetArrayLength().ShouldBe(1);
		parsed.RootElement[0].GetProperty("op").GetString().ShouldBe("replace");
	}

	[RetryFact(3)]
	public void RoundTrip_SerializeThenDeserialize_PreservesOperations()
	{
		// Arrange
		JsonPatchDocument<JsonPatchTestEntity> original = new();
		original.Replace(e => e.Name, "RoundTrip");

		// Act
		string json = JsonSerializer.Serialize(original, options);
		JsonPatchDocument<JsonPatchTestEntity>? result = JsonSerializer.Deserialize<JsonPatchDocument<JsonPatchTestEntity>>(json, options);

		// Assert
		result.ShouldNotBeNull();
		result.Operations.Count.ShouldBe(original.Operations.Count);
		result.Operations[0].op.ShouldBe(original.Operations[0].op);
		result.Operations[0].path.ShouldBe(original.Operations[0].path);
	}
}

public sealed class JsonPatchEndpointSupportExtensionsTests
{
	[RetryFact(3)]
	public void UseJsonPatchRequestBody_AddsMiddlewareAndReturnsBuilder()
	{
		// Arrange
		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseJsonPatchRequestBody();

		// Assert
		result.ShouldBe(builder);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).MustHaveHappened();
	}
}

file sealed class JsonPatchTestEntity
{
	public string Name { get; set; } = string.Empty;
	public int Age { get; set; }
}
