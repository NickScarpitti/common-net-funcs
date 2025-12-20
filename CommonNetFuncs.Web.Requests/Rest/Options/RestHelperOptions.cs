using System.Text.Json;

namespace CommonNetFuncs.Web.Requests.Rest.Options;

public sealed class RestHelperOptions
{
	public RestHelperOptions(string Url, string ApiName, IDictionary<string, string>? HttpHeaders = null, bool UseBearerToken = false, string? BearerToken = null,
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

	public string Url { get; set; }

	public string ApiName { get; set; }

	public IDictionary<string, string>? HttpHeaders { get; set; }

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
