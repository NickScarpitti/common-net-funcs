using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;


namespace CommonNetFuncs.Web.Middleware;

/// <summary>
/// Bridges the gap between JSON Patch requests (Content-Type: application/json-patch+json)
/// and minimal API endpoints that accept <see cref="JsonPatchDocument{T}"/> parameters.
///
/// <para>
/// The minimal API parameter-binding pipeline reads any <c>application/*+json</c> content
/// type using System.Text.Json. Because <see cref="JsonPatchDocument{T}"/> is a
/// Newtonsoft.Json type whose on-wire format (a JSON array of operations) is not natively
/// understood by STJ, a <see cref="JsonPatchDocumentConverterFactory"/> bridges the two
/// serializers: STJ captures the raw JSON and hands it to Newtonsoft.Json, which has the
/// required converters to construct the document and strongly-type each operation's value
/// field. Without this, complex object values in patch operations are left as
/// <see cref="JsonElement"/> and fail when the patch is applied.
/// </para>
///
/// <para>
/// <see cref="JsonPatchRequestMiddleware"/> additionally normalizes the Content-Type header
/// from <c>application/json-patch+json</c> to <c>application/json</c> as an explicit safety
/// net, since <c>application/*+json</c> is matched by ASP.NET Core's body-binding pipeline
/// but some hosts or proxies may strip or alter structured-syntax suffixes.
/// </para>
/// </summary>
public sealed class JsonPatchRequestMiddleware(RequestDelegate next)
{
	private const string JsonPatchMime = "application/json-patch+json";
	private const string JsonMime = "application/json; charset=utf-8";

	public Task InvokeAsync(HttpContext context)
	{
		if ((context.Request.ContentType ?? string.Empty).Contains(JsonPatchMime, StringComparison.OrdinalIgnoreCase))
		{
			context.Request.ContentType = JsonMime;
		}
		return next(context);
	}
}

/// <summary>
/// System.Text.Json <see cref="JsonConverterFactory"/> for open-generic
/// <see cref="JsonPatchDocument{T}"/> instances. Creates a closed
/// <see cref="JsonPatchDocumentConverter{T}"/> for each concrete entity type.
/// </summary>
public sealed class JsonPatchDocumentConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
		=> typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(JsonPatchDocument<>);

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		Type entityType = typeToConvert.GetGenericArguments()[0];
		return (JsonConverter)Activator.CreateInstance(
			typeof(JsonPatchDocumentConverter<>).MakeGenericType(entityType))!;
	}
}

/// <summary>
/// Reads and writes <see cref="JsonPatchDocument{T}"/> via a raw-JSON bridge to
/// Newtonsoft.Json. STJ captures the token stream as a raw text string;
/// Newtonsoft.Json deserializes it using its built-in <c>JsonPatchDocument</c> converter,
/// which correctly maps the JSON Patch array format and strongly-types each operation's
/// <c>value</c> field according to the properties of <typeparamref name="T"/>.
/// </summary>
public sealed class JsonPatchDocumentConverter<T> : JsonConverter<JsonPatchDocument<T>> where T : class
{
	public override JsonPatchDocument<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using JsonDocument doc = JsonDocument.ParseValue(ref reader);
		return Newtonsoft.Json.JsonConvert.DeserializeObject<JsonPatchDocument<T>>(doc.RootElement.GetRawText());
	}

	public override void Write(Utf8JsonWriter writer, JsonPatchDocument<T> value, JsonSerializerOptions options)
	{
		using JsonDocument doc = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(value));
		doc.WriteTo(writer);
	}
}

public static class JsonPatchEndpointSupportExtensions
{
	/// <summary>
	/// Adds <see cref="JsonPatchRequestMiddleware"/> to the pipeline to normalize
	/// <c>application/json-patch+json</c> Content-Type headers to <c>application/json</c>.
	/// Call this early in the pipeline, before routing so the rewrite occurs before
	/// parameter binding reads the request body.
	/// </summary>
	public static IApplicationBuilder UseJsonPatchRequestBody(this IApplicationBuilder app)
		=> app.UseMiddleware<JsonPatchRequestMiddleware>();
}
