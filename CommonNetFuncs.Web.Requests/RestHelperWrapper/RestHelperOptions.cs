using System.Text.Json;

namespace CommonNetFuncs.Web.Requests.RestHelperWrapper;

public sealed class RestHelperOptions
{
    public RestHelperOptions(string Url, string ApiName, Dictionary<string, string>? HttpHeaders = null, bool UseBearerToken = false, string? BearerToken = null,
        bool UseNewtonsoftDeserializer = false, bool LogQuery = true, bool LogBody = true, CompressionOptions? CompressionOptions = null, MsgPackOptions? MsgPackOptions = null,
        JsonSerializerOptions? JsonSerializerOptions = null, ResilienceOptions? ResilienceOptions = null)
    {
        if (string.IsNullOrWhiteSpace(ApiName))
        {
            throw new ArgumentException("ApiName cannot be null or whitespace", nameof(ApiName));
        }

        if (string.IsNullOrWhiteSpace(Url))
        {
            throw new ArgumentException("Url cannot be null or whitespace", nameof(Url));
        }

        if (string.IsNullOrWhiteSpace(BearerToken) && UseBearerToken && ResilienceOptions?.GetBearerTokenFunc == null)
        {
            throw new ArgumentException("BearerToken cannot be null or whitespace when UseBearerToken is true and CustomResilienceOptions.GetBearerTokenFunc is null.", nameof(BearerToken));
        }

        this.Url = Url;
        this.ApiName = ApiName;
        this.HttpHeaders = HttpHeaders;
        this.UseBearerToken = UseBearerToken;
        this.UseNewtonsoftDeserializer = UseNewtonsoftDeserializer;
        this.LogQuery = LogQuery;
        this.LogBody = LogBody;
        this.CompressionOptions = CompressionOptions;
        this.ResilienceOptions = ResilienceOptions;
        this.BearerToken = BearerToken;
        this.MsgPackOptions = MsgPackOptions;
        this.JsonSerializerOptions = JsonSerializerOptions;
    }

    // Causes conflicts
    //public RestHelperOptions(string Url, string ApiName, Dictionary<string, string>? HttpHeaders = null, bool UseBearerToken = false, string? BearerToken = null,
    //    bool UseNewtonsoftDeserializer = false, bool LogQuery = true, bool LogBody = true, CompressionOptions? CompressionOptions = null, int MaxRetry = 10, int RetryDelay = 1000, long?
    //    TimeoutValue = 100, bool RunOnce = false, bool NullOk = false, Func<HttpStatusCode, bool>? ShouldRetryByStatusFunc = null, DelayBackoffType DelayBackoffType = DelayBackoffType.Constant,
    //    bool UseJitter = true, Func<HttpResponseMessage, ResilienceOptions, bool>? ShouldRetryFunc = null, MsgPackOptions? MsgPackOptions = null, JsonSerializerOptions? JsonSerializerOptions = null,
    //    Func<string, bool, ValueTask<string>>? GetBearerTokenFunc = null)
    //{
    //    if (string.IsNullOrWhiteSpace(ApiName))
    //    {
    //        throw new ArgumentException("ApiName cannot be null or whitespace", nameof(ApiName));
    //    }

    //    if (string.IsNullOrWhiteSpace(Url))
    //    {
    //        throw new ArgumentException("Url cannot be null or whitespace", nameof(Url));
    //    }

    //    if (string.IsNullOrWhiteSpace(BearerToken) && UseBearerToken && ResilienceOptions?.GetBearerTokenFunc == null)
    //    {
    //        throw new ArgumentException("BearerToken cannot be null or whitespace when UseBearerToken is true and CustomResilienceOptions.GetBearerTokenFunc is null.", nameof(BearerToken));
    //    }

    //    this.Url = Url;
    //    this.ApiName = ApiName;
    //    this.HttpHeaders = HttpHeaders;
    //    this.UseBearerToken = UseBearerToken;
    //    this.UseNewtonsoftDeserializer = UseNewtonsoftDeserializer;
    //    this.LogQuery = LogQuery;
    //    this.LogBody = LogBody;
    //    this.CompressionOptions = CompressionOptions;
    //    this.BearerToken = BearerToken;
    //    this.MsgPackOptions = MsgPackOptions;
    //    this.JsonSerializerOptions = JsonSerializerOptions;

    //    ResilienceOptions = new(MaxRetry, RetryDelay, TimeoutValue, RunOnce, NullOk, ShouldRetryByStatusFunc, DelayBackoffType, UseJitter, ShouldRetryFunc, GetBearerTokenFunc);
    //}

    public string Url { get; set; }

    public string ApiName { get; set; }

    public Dictionary<string, string>? HttpHeaders { get; set; }

    public bool UseBearerToken { get; set; }

    public string? BearerToken { get; set; }

    public bool UseNewtonsoftDeserializer { get; set; }

    public bool LogQuery { get; set; }

    public bool LogBody { get; set; }

    public CompressionOptions? CompressionOptions { get; set; }

    public MsgPackOptions? MsgPackOptions { get; set; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public ResilienceOptions? ResilienceOptions { get; set; }
}
