using System.Text.Json;
using CommonNetFuncs.Web.Common;

namespace CommonNetFuncs.Web.Requests.Rest;

public sealed class RequestOptions<T>
{
	public string Url { get; set; } = null!;

	public string RedactedUrl
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Url))
			{
				return Url;
			}
			try
			{
				return Url.GetRedactedUri();
			}
			catch
			{
				return "<Error Redacting URL>";
			}
		}
	}

	public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

	public string? BearerToken { get; set; }

	public double? Timeout { get; set; }

	public Dictionary<string, string>? HttpHeaders { get; set; }

	public JsonSerializerOptions? JsonSerializerOptions { get; set; }

	public bool UseNewtonsoftDeserializer { get; set; }

	public bool ExpectTaskCancellation { get; set; }

	public bool LogRequest { get; set; }

	public bool LogQuery { get; set; }

	public bool LogBody { get; set; }

	public bool LogResponse { get; set; }

	public MsgPackOptions? MsgPackOptions { get; set; }

	public HttpContent? PatchDocument { get; set; }

	public T? BodyObject { get; set; }
}

public interface IRestHelpersCommon
{
	Task<T?> RestRequest<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default);

	IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default);

	Task<RestObject<T>> RestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default);

	Task<StreamingRestObject<T>> StreamingRestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default);
}

public class RestHelpersCommon(HttpClient client) : IRestHelpersCommon
{
	public readonly HttpClient client = client;

	public Task<T?> RestRequest<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.RestRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.StreamingRestRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public Task<RestObject<T>> RestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.RestObjectRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public Task<StreamingRestObject<T>> StreamingRestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.StreamingRestObjectRequest<T, UT>(baseRequestOptions, cancellationToken);
	}
}

public class RestHelpersCommonFactory(IHttpClientFactory httpClientFactory, string? httpClientName = null) : IRestHelpersCommon, IDisposable
{
	public readonly HttpClient client = string.IsNullOrWhiteSpace(httpClientName) ? httpClientFactory.CreateClient() : httpClientFactory.CreateClient(httpClientName);

	public Task<T?> RestRequest<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.RestRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.StreamingRestRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public Task<RestObject<T>> RestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.RestObjectRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public Task<StreamingRestObject<T>> StreamingRestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions, CancellationToken cancellationToken = default)
	{
		return client.StreamingRestObjectRequest<T, UT>(baseRequestOptions, cancellationToken);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && client != null)
		{
			client.Dispose();
		}
	}

	~RestHelpersCommonFactory()
	{
		Dispose(false);
	}
}
