namespace CommonNetFuncs.Web.Requests.Rest;

/// <summary>
/// Helper functions that send requests to specified URI and return resulting values where applicable
/// Source1: https://medium.com/@srikanth.gunnala/generic-wrapper-to-consume-asp-net-web-api-rest-service-641b50462c0
/// Source2: https://stackoverflow.com/questions/43692053/how-can-i-create-a-jsonpatchdocument-from-comparing-two-c-sharp-objects
/// </summary>
public sealed class RestHelpers() : RestHelpersCommon(client)
{
  private static readonly SocketsHttpHandler socketsHttpHandler = new() { MaxConnectionsPerServer = 100, KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
         KeepAlivePingDelay = TimeSpan.FromSeconds(15), KeepAlivePingTimeout = TimeSpan.FromMinutes(60) };
  private static new readonly HttpClient client = new(socketsHttpHandler) { Timeout = Timeout.InfiniteTimeSpan }; //Use infinite timespan here to force using token specified timeout
}
