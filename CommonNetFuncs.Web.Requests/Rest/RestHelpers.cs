namespace CommonNetFuncs.Web.Requests.Rest;

/// <summary>
/// Helper functions that send requests to specified URI and return resulting values where applicable
/// </summary>
public sealed class RestHelpers() : RestHelpersCommon(client)
{
	private static readonly SocketsHttpHandler socketsHttpHandler = new()
	{
		MaxConnectionsPerServer = 100,
		KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
		KeepAlivePingDelay = TimeSpan.FromSeconds(15),
		KeepAlivePingTimeout = TimeSpan.FromMinutes(60)
	};
	private static new readonly HttpClient client = new(socketsHttpHandler) { Timeout = Timeout.InfiniteTimeSpan }; //Use infinite timespan here to force using token specified timeout
}
