namespace CommonNetFuncs.Web.Requests.Rest;

/// <summary>
/// Helper class to get around not being able to pass primitive types directly to a generic type
/// </summary>
/// <typeparam name="T">Primitive type to pass to the REST request</typeparam>
public sealed class RestObject<T>// where T : class
{
	public T? Result { get; set; }

	public HttpResponseMessage? Response { get; set; }

	public string? Error { get; set; }
}

/// <summary>
/// Helper class to get around not being able to pass primitive types directly to a generic type
/// </summary>
/// <typeparam name="T">Primitive type to pass to the REST request</typeparam>
public sealed class StreamingRestObject<T>// where T : class
{
	public IAsyncEnumerable<T?>? Result { get; set; }

	public HttpResponseMessage? Response { get; set; }
}
