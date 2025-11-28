using System.Collections.Immutable;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Common.EncodingTypes;

namespace CommonNetFuncs.Web.Requests.Rest;

public static class RestHelperConstants
{
	public enum EDelayBackoffType
	{
		Constant,
		Linear,
		Exponential
	}

	public const string ContentTypeHeader = "Content-Type";
	public const string AcceptEncodingHeader = "Accept-Encoding";
	public const string AcceptHeader = "Accept";

	public static readonly KeyValuePair<string, string> NoEncodingHeader = new(AcceptEncodingHeader, Identity);
	public static readonly KeyValuePair<string, string> BrotliEncodingHeader = new(AcceptEncodingHeader, Brotli);
	public static readonly KeyValuePair<string, string> GzipEncodingHeader = new(AcceptEncodingHeader, GZip);

	public static readonly KeyValuePair<string, string> MemPackContentHeader = new(ContentTypeHeader, MemPack);
	public static readonly KeyValuePair<string, string> MsgPackContentHeader = new(ContentTypeHeader, MsgPack);
	public static readonly KeyValuePair<string, string> JsonContentHeader = new(ContentTypeHeader, Json);

	public static readonly KeyValuePair<string, string> MemPackAcceptHeader = new(AcceptHeader, MemPack);
	public static readonly KeyValuePair<string, string> MsgPackAcceptHeader = new(AcceptHeader, MsgPack);
	public static readonly KeyValuePair<string, string> JsonAcceptHeader = new(AcceptHeader, Json);

	public static readonly IImmutableDictionary<string, string> MemPackHeaders = ImmutableDictionary.CreateRange([MemPackContentHeader, MemPackAcceptHeader]);
	public static readonly IImmutableDictionary<string, string> MsgPackHeaders = ImmutableDictionary.CreateRange([MsgPackContentHeader, MsgPackAcceptHeader]);
	public static readonly IImmutableDictionary<string, string> JsonHeaders = ImmutableDictionary.CreateRange([JsonContentHeader, JsonAcceptHeader]);
	public static readonly IImmutableDictionary<string, string> JsonNoEncodeHeaders = ImmutableDictionary.CreateRange([JsonContentHeader, JsonAcceptHeader, NoEncodingHeader]);
}
