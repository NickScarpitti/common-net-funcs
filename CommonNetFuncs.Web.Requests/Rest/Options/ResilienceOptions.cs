using System.Net;
using Polly;

namespace CommonNetFuncs.Web.Requests.Rest.Options;

public class ResilienceOptions(int MaxRetry = 10, int RetryDelay = 1000, long? TimeoutValue = 100, bool RunOnce = false, bool NullOk = false,
    Func<HttpStatusCode, bool>? ShouldRetryByStatusFunc = null, DelayBackoffType DelayBackoffType = DelayBackoffType.Constant, bool UseJitter = true,
    Func<HttpResponseMessage, ResilienceOptions, bool>? ShouldRetryFunc = null, Func<string, bool, ValueTask<string>>? GetBearerTokenFunc = null)
{
    public bool NullOk { get; set; } = NullOk;

    public bool RunOnce { get; set; } = RunOnce;

    public int MaxRetry { get; set; } = MaxRetry;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(RetryDelay);

    public TimeSpan? TimeoutValue
    {
        get;
        set;
    } = TimeSpan.FromSeconds(TimeoutValue == null || TimeoutValue < 0 ? 100 : (long)TimeoutValue);

    public Func<HttpStatusCode, bool>? ShouldRetryByStatusFunc { get; set; } = ShouldRetryByStatusFunc;

    public DelayBackoffType DelayBackoffType { get; set; } = DelayBackoffType;

    public bool UseJitter { get; set; } = UseJitter;

    public Func<HttpResponseMessage, ResilienceOptions, bool>? ShouldRetryFunc { get; set; } = ShouldRetryFunc;

    /// <summary>
    /// This function is called to get a bearer token if UseBearerToken is true and BearerToken is not set.
    /// </summary>
    /// <remarks>
    /// The first parameter is the ApiName, the second parameter indicates if the token should be forcefully refreshed (true on retry).
    /// </remarks>
    public Func<string, bool, ValueTask<string>>? GetBearerTokenFunc { get; set; } = GetBearerTokenFunc;
}
