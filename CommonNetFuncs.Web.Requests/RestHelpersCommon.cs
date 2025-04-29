using System.Text.Json;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Web.Requests;

public sealed class RequestOptions<T>
{
    public string Url { get; set; } = null!;
    public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
    public string? BearerToken { get; set; }
    public double? Timeout { get; set; }
    public Dictionary<string, string>? HttpHeaders { get; set; }
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    public bool UseNewtonsoftDeserializer { get; set; }
    public bool ExpectTaskCancellation { get; set; }
    public bool LogQuery { get; set; }
    public bool LogBody { get; set; }
    public MsgPackOptions? MsgPackOptions { get; set; }
    public HttpContent? PatchDocument { get; set; }
    public T? BodyObject { get; set; }
}

public interface IRestHelpersCommon
{
    Task<T?> RestRequest<T, UT>(RequestOptions<UT> baseRequestOptions);
    IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(RequestOptions<UT> baseRequestOptions);
    Task<RestObject<T>> RestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions);
    Task<StreamingRestObject<T>> StreamingRestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions);
}

public class RestHelpersCommon(HttpClient client) : IRestHelpersCommon
{
    public readonly HttpClient client = client;

    public Task<T?> RestRequest<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.RestRequest<T, UT>(baseRequestOptions);
    }

    public IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.StreamingRestRequest<T, UT>(baseRequestOptions);
    }

    public Task<RestObject<T>> RestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.RestObjectRequest<T, UT>(baseRequestOptions);
    }

    public Task<StreamingRestObject<T>> StreamingRestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.StreamingRestObjectRequest<T, UT>(baseRequestOptions);
    }
}

public class RestHelpersCommonFactory(IHttpClientFactory httpClientFactory, string? httpClientName = null) : IRestHelpersCommon, IDisposable
{
    public readonly HttpClient client = httpClientName.IsNullOrWhiteSpace() ? httpClientFactory.CreateClient() : httpClientFactory.CreateClient(httpClientName);
    public Task<T?> RestRequest<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.RestRequest<T, UT>(baseRequestOptions);
    }

    public IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.StreamingRestRequest<T, UT>(baseRequestOptions);
    }

    public Task<RestObject<T>> RestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.RestObjectRequest<T, UT>(baseRequestOptions);
    }

    public Task<StreamingRestObject<T>> StreamingRestRequestObject<T, UT>(RequestOptions<UT> baseRequestOptions)
    {
        return client.StreamingRestObjectRequest<T, UT>(baseRequestOptions);
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
