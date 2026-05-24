using System.Runtime.CompilerServices;
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

namespace CommonNetFuncs.Web.Requests;

public sealed class RestHelper(RestHelperOptions defaultOptions, RestHelpersWrapper restHelpersWrapper)
{
	private readonly RestHelperOptions defaultOptions = defaultOptions;

	private void FillDefaultOptions(RestHelperOptions options)
	{
		options.ResilienceOptions ??= new();
		options.ResilienceOptions.GetBearerTokenFunc ??= defaultOptions.ResilienceOptions?.GetBearerTokenFunc;
		options.UseBearerToken = options.UseBearerToken || defaultOptions.UseBearerToken;
		options.JsonSerializerOptions ??= defaultOptions.JsonSerializerOptions;
		options.MsgPackOptions ??= defaultOptions.MsgPackOptions;
		options.CompressionOptions ??= defaultOptions.CompressionOptions;
	}

	public async Task<T?> Get<T>(RestHelperOptions options, CancellationToken cancellationToken = default)
	{
		FillDefaultOptions(options);
		return await restHelpersWrapper.Get<T>(options, cancellationToken);
	}

	public async IAsyncEnumerable<T?> GetStreaming<T>(RestHelperOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		FillDefaultOptions(options);
		await foreach (T? item in restHelpersWrapper.GetStreaming<T>(options, cancellationToken))
		{
			yield return item;
		}
	}

	public async Task<T?> PostRequest<T>(RestHelperOptions options, T postObject)
	{
		FillDefaultOptions(options);
		return await restHelpersWrapper.PostRequest(options, postObject);
	}

	public async IAsyncEnumerable<T?> PostRequestStreaming<T>(RestHelperOptions options, T postObject, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		FillDefaultOptions(options);
		await foreach (T? item in restHelpersWrapper.PostRequestStreaming(options, postObject, cancellationToken))
		{
			yield return item;
		}
	}

	public async Task<T?> GenericPostRequest<T, UT>(RestHelperOptions options, UT postObject)
	{
		FillDefaultOptions(options);
		return await restHelpersWrapper.GenericPostRequest<T, UT>(options, postObject);
	}

	public async IAsyncEnumerable<T?> GenericPostRequestStreaming<T, UT>(RestHelperOptions options, UT postObject, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		FillDefaultOptions(options);
		await foreach (T? item in restHelpersWrapper.GenericPostRequestStreaming<T, UT>(options, postObject, cancellationToken))
		{
			yield return item;
		}
	}

	public async Task<string?> StringPostRequest<T>(RestHelperOptions options, T postObject)
	{
		FillDefaultOptions(options);
		return await restHelpersWrapper.StringPostRequest(options, postObject);
	}

	public async Task<T?> PatchRequest<T>(RestHelperOptions options, T model, T oldModel) where T : class
	{
		FillDefaultOptions(options);
		return await restHelpersWrapper.PatchRequest(options, model, oldModel);
	}
}
