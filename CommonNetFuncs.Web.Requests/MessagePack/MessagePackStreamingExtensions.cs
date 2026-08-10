using MessagePack;
using Microsoft.AspNetCore.Mvc;

namespace CommonNetFuncs.Web.Requests.MessagePack;

/// <summary>
/// Extension methods for streaming MessagePack responses from controllers.
/// </summary>
public static class MessagePackStreamingExtensions
{
	/// <summary>
	/// Creates a MessagePackStreamingResult that streams IAsyncEnumerable data as MessagePack chunks.
	/// The client can use MessagePackStreamReader to reconstruct the stream as IAsyncEnumerable on their end.
	/// </summary>
	/// <typeparam name="T">Type of items to stream</typeparam>
	/// <param name="controller">Controller base instance</param>
	/// <param name="data">The async enumerable data to stream</param>
	/// <param name="options">Optional MessagePack serializer options</param>
	/// <param name="successStatusCode">Status code to return when data is present (default: 200 OK)</param>
	/// <param name="emptyStatusCode">Status code to return when no data is present (default: 204 No Content)</param>
	/// <returns>MessagePackStreamingResult that can be returned from a controller action</returns>
	public static MessagePackStreamingResult<T> StreamMessagePack<T>(this ControllerBase controller, IAsyncEnumerable<T> data, MessagePackSerializerOptions? options = null, 
		int? successStatusCode = null, int? emptyStatusCode = null)
	{
		return new MessagePackStreamingResult<T>(data, options, successStatusCode, emptyStatusCode);
	}
}
