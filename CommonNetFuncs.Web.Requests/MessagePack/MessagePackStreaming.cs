using System.Text.Json;
using MessagePack;
using Microsoft.AspNetCore.Http;

namespace CommonNetFuncs.Web.Requests.MessagePack;

/// <summary>
/// Static helper methods for creating MessagePackStreamingResult in Minimal APIs or without controller instances.
/// </summary>
public static class MessagePackStreaming
{
	/// <summary>
	/// Creates a MessagePackStreamingResult that streams IAsyncEnumerable data with content negotiation.
	/// Supports MessagePack (application/x-msgpack) and NDJSON (application/json) streaming based on Accept header.
	/// Defaults to JSON when no Accept header is specified or */* is requested.
	/// Use this in Minimal APIs or when you don't have access to a ControllerBase instance.
	/// Returns 400 Bad Request for unsupported Accept headers.
	/// </summary>
	/// <typeparam name="T">Type of items to stream</typeparam>
	/// <param name="data">The async enumerable data to stream</param>
	/// <param name="messagePackOptions">Optional MessagePack serializer options</param>
	/// <param name="jsonOptions">Optional JSON serializer options</param>
	/// <param name="successStatusCode">Status code to return when data is present (default: 200 OK)</param>
	/// <param name="emptyStatusCode">Status code to return when no data is present (default: 204 No Content)</param>
	/// <returns>MessagePackStreamingResult that can be returned from a minimal API endpoint or controller action</returns>
	public static MessagePackStreamingResult<T> Stream<T>(
		IAsyncEnumerable<T> data,
		MessagePackSerializerOptions? messagePackOptions = null,
		JsonSerializerOptions? jsonOptions = null,
		int successStatusCode = StatusCodes.Status200OK,
		int emptyStatusCode = StatusCodes.Status204NoContent)
	{
		return new MessagePackStreamingResult<T>(data, messagePackOptions, jsonOptions, successStatusCode, emptyStatusCode);
	}
}
