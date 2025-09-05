using CommonNetFuncs.Core;
using NLog;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;

namespace CommonNetFuncs.Web.Requests.RestHelperWrapper;

internal static class Headers
{
    public static Dictionary<string, string> GetHeaders(RestHelperOptions options, bool isStreaming)
    {
        Dictionary<string, string> headers = options.HttpHeaders == null ? new Dictionary<string, string>() : new Dictionary<string, string>(options.HttpHeaders);
        return SetCompressionHttpHeaders(headers, options.CompressionOptions, isStreaming).ToDictionary();
    }

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<string, string> MemPackHeadersWithGzip = new([MemPackContentHeader, MemPackAcceptHeader, GzipEncodingHeader]);
    private static readonly Dictionary<string, string> MemPackHeadersWithBrotli = new([MemPackContentHeader, MemPackAcceptHeader, BrotliEncodingHeader]);

    private static readonly Dictionary<string, string> MsgPackHeadersWithGzip = new([MsgPackContentHeader, MsgPackAcceptHeader, GzipEncodingHeader]);
    private static readonly Dictionary<string, string> MsgPackHeadersWithBrotli = new([MsgPackContentHeader, MsgPackAcceptHeader, BrotliEncodingHeader]);

    private static readonly Dictionary<string, string> JsonHeadersWithGzip = new([JsonContentHeader, JsonAcceptHeader, GzipEncodingHeader]);
    private static readonly Dictionary<string, string> JsonHeadersWithBrotli = new([JsonContentHeader, JsonAcceptHeader, BrotliEncodingHeader]);

    internal static Dictionary<string, string> SetCompressionHttpHeaders(Dictionary<string, string>? httpHeaders, CompressionOptions? compressionOptions = null, bool isStreaming = false)
    {
        Dictionary<string, string>? compressionHeaders = [];
        try
        {
            if (!isStreaming)
            {
                if (compressionOptions?.UseCompression == true)
                {
                    if (compressionOptions.CompressionType != null)
                    {
                        if (compressionOptions.CompressionType == ECompressionType.Gzip)
                        {
                            if (compressionOptions.UseMemPack)
                            {
                                compressionHeaders = new(MemPackHeadersWithGzip);
                            }
                            else if (compressionOptions.UseMsgPack)
                            {
                                compressionHeaders = new(MsgPackHeadersWithGzip);
                            }
                            else
                            {
                                compressionHeaders = new(JsonHeadersWithGzip);
                            }
                        }
                        else if (compressionOptions.CompressionType == ECompressionType.Brotli)
                        {
                            if (compressionOptions.UseMemPack)
                            {
                                compressionHeaders = new(MemPackHeadersWithBrotli);
                            }
                            else if (compressionOptions.UseMsgPack)
                            {
                                compressionHeaders = new(MsgPackHeadersWithBrotli);
                            }
                            else
                            {
                                compressionHeaders = new(JsonHeadersWithBrotli);
                            }
                        }
                        else
                        {
                            if (compressionOptions.UseMemPack)
                            {
                                compressionHeaders = new(MemPackHeadersWithGzip);
                            }
                            else if (compressionOptions.UseMsgPack)
                            {
                                compressionHeaders = new(MsgPackHeadersWithGzip);
                            }
                            else
                            {
                                compressionHeaders = new(JsonHeadersWithGzip);
                            }
                        }
                    }
                    else
                    {
                        if (compressionOptions.UseMemPack)
                        {
                            compressionHeaders = new(MemPackHeaders);
                        }
                        else if (compressionOptions.UseMsgPack)
                        {
                            compressionHeaders = new(MsgPackHeaders);
                        }
                        else
                        {
                            compressionHeaders = new(JsonHeaders);
                        }
                    }
                }
            }
            else
            {
                compressionHeaders = new(JsonNoEncodeHeaders); //Need to use JSON with no compression when streaming data
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (httpHeaders.AnyFast())
        {
            foreach (KeyValuePair<string, string> header in compressionHeaders)
            {
                if (!httpHeaders.ContainsKey(header.Key))
                {
                    httpHeaders.TryAdd(header.Key, header.Value);
                }
                //httpHeaders.AddOrUpdate(header.Key, header.Value, (_, __) => header.Value);
            }
            return httpHeaders ?? [];
        }
        return compressionHeaders;
    }
}
