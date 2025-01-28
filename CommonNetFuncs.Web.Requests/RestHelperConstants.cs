using CommonNetFuncs.Web.Common;

namespace CommonNetFuncs.Web.Requests;

public static class RestHelperConstants
{
    public const string ContentTypeHeader = "Content-Type";
    public const string AcceptEncodingHeader = "Accept-Encoding";
    public const string AcceptHeader = "Accept";

    public static readonly KeyValuePair<string, string> NoEncodingHeader = new(AcceptEncodingHeader, EncodingTypes.Identity);
    public static readonly KeyValuePair<string, string> BrotliEncodingHeader = new(AcceptEncodingHeader, EncodingTypes.Brotli);
    public static readonly KeyValuePair<string, string> GzipEncodingHeader = new(AcceptEncodingHeader, EncodingTypes.GZip);

    public static readonly KeyValuePair<string, string> MemPackContentHeader = new(ContentTypeHeader, ContentTypes.MemPack);
    public static readonly KeyValuePair<string, string> MsgPackContentHeader = new(ContentTypeHeader, ContentTypes.MsgPack);
    public static readonly KeyValuePair<string, string> JsonContentHeader = new(ContentTypeHeader, ContentTypes.Json);

    public static readonly KeyValuePair<string, string> MemPackAcceptHeader = new(AcceptHeader, ContentTypes.MemPack);
    public static readonly KeyValuePair<string, string> MsgPackAcceptHeader = new(AcceptHeader, ContentTypes.MsgPack);
    public static readonly KeyValuePair<string, string> JsonAcceptHeader = new(AcceptHeader, ContentTypes.Json);

    public static readonly Dictionary<string, string> MemPackHeaders = new([MemPackContentHeader, MemPackAcceptHeader]);
    public static readonly Dictionary<string, string> MsgPackHeaders = new([MsgPackContentHeader, MsgPackAcceptHeader]);
    public static readonly Dictionary<string, string> JsonHeaders = new([JsonContentHeader, JsonAcceptHeader]);
    public static readonly Dictionary<string, string> JsonNoEncodeHeaders = new([JsonContentHeader, JsonAcceptHeader, NoEncodingHeader]);
}
