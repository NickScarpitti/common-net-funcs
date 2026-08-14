using System.Text.Json;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CommonNetFuncs.Web.Requests.MessagePack;

/// <summary>
/// Extension methods for streaming responses from controllers with content negotiation support.
/// </summary>
public static class MessagePackStreamingExtensions
{
	/// <summary>
	/// Creates a MessagePackStreamingResult that streams IAsyncEnumerable data with content negotiation.
	/// Supports MessagePack (application/x-msgpack) and NDJSON (application/json) streaming based on Accept header.
	/// Defaults to JSON when no Accept header is specified or */* is requested.
	/// Returns 400 Bad Request for unsupported Accept headers.
	/// </summary>
	/// <typeparam name="T">Type of items to stream</typeparam>
	/// <param name="controller">Controller base instance</param>
	/// <param name="data">The async enumerable data to stream</param>
	/// <param name="messagePackOptions">Optional MessagePack serializer options</param>
	/// <param name="jsonOptions">Optional JSON serializer options</param>
	/// <param name="successStatusCode">Status code to return when data is present (default: 200 OK)</param>
	/// <param name="emptyStatusCode">Status code to return when no data is present (default: 204 No Content)</param>
	/// <returns>MessagePackStreamingResult that can be returned from a controller action</returns>
#pragma warning disable RCS1175 // Unused 'this' parameter
#pragma warning disable IDE0060 // Remove unused parameter
	public static MessagePackStreamingResult<T> StreamMessagePack<T>(
		this ControllerBase controller,
		IAsyncEnumerable<T> data,
		MessagePackSerializerOptions? messagePackOptions = null,
		JsonSerializerOptions? jsonOptions = null,
		int successStatusCode = StatusCodes.Status200OK,
		int emptyStatusCode = StatusCodes.Status204NoContent)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1175 // Unused 'this' parameter
	{
		return new MessagePackStreamingResult<T>(data, messagePackOptions, jsonOptions, successStatusCode, emptyStatusCode);
	}
}
