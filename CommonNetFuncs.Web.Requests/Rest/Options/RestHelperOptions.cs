using System.Text.Json;
using MessagePack;

namespace CommonNetFuncs.Web.Requests.Rest.Options;

public sealed class RestHelperOptions
{
	public RestHelperOptions(string Url, string ApiName, IDictionary<string, string>? HttpHeaders = null, bool UseBearerToken = false, string? BearerToken = null,
			bool UseNewtonsoftDeserializer = false, bool LogQuery = true, bool LogBody = true, CompressionOptions? CompressionOptions = null, MessagePackSerializerOptions? MessagePackSerializerOptions = null,
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
		this.MessagePackSerializerOptions = MessagePackSerializerOptions;
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

	public MessagePackSerializerOptions? MessagePackSerializerOptions { get; set; }

	public JsonSerializerOptions? JsonSerializerOptions { get; set; }

	public ResilienceOptions? ResilienceOptions { get; set; }
}

public sealed class RestHelperOptionsDefaultConfig
{
	/// <summary>
	/// If set, will always use this value and ignore the value from the options passed to the <see cref="RestHelper"/> methods.
	/// This is useful for setting a default value that should always be used, regardless of what is passed in the options.
	/// For example, if you want to always use a bearer token for authentication, you can set this to true and it will override any value passed in the options.
	/// If not set, it will use the value from the options passed to the <see cref="RestHelper"/> methods.
	/// </summary>
	public bool? UseBearerToken { get; set; }

	/// <summary>
	/// If set, will be used if <see cref="CompressionOptions"/> is <see langword="null"/> in the options passed to the <see cref="RestHelper"/> methods.
	/// This allows you to set a default compression option that will be used if not specified in the individual method calls.
	/// </summary>
	public CompressionOptions? CompressionOptions { get; set; }

	/// <summary>
	/// If set, will be used if  <see cref="MessagePackSerializerOptions"/> is <see langword="null"/> in the options passed to the <see cref="RestHelper"/> methods.
	/// This allows you to set a default MsgPack option that will be used if not specified in the individual method calls.
	/// </summary>
	public MessagePackSerializerOptions? MessagePackSerializerOptions { get; set; }

	/// <summary>
	/// If set, will be used if <see cref="JsonSerializerOptions"/> is <see langword="null"/> in the options passed to the <see cref="RestHelper"/> methods.
	/// This allows you to set a default JsonSerializer option that will be used if not specified in the individual method calls.
	/// </summary>
	public JsonSerializerOptions? JsonSerializerOptions { get; set; }

	/// <summary>
	/// If set, will be used if <see cref="ResilienceOptions"/> is <see langword="null"/> in the options passed to the <see cref="RestHelper"/> methods.
	/// This allows you to set a default resilience option that will be used if not specified in the individual method calls.
	/// </summary>
	public ResilienceOptions? ResilienceOptions { get; set; }
}
