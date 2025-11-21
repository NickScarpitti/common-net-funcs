namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

/// <summary>
/// Factory for creating IRestClient instances.
/// </summary>
public interface IRestClientFactory
{
	/// <summary>
	/// Creates a new IRestClient instance for the specified API.
	/// </summary>
	/// <param name="apiName">The name of the API configuration to use.</param>
	/// <returns>A new IRestClient instance.</returns>
	IRestClient CreateClient(string apiName);
}
