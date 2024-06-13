namespace Common_Net_Funcs.Web;

/// <summary>
/// Common content types
/// </summary>
public static class ContentTypes
{
    public const string Json = "application/json";
    public const string JsonProblem = "application/problem+json";
    public const string MemPack = "application/x-memorypack";
    public const string MsgPack = "application/x-msgpack";

    //Images
    public const string Png = "image/png";
    public const string Jpeg = "image/jpeg";
    public const string Gif = "image/gif";
    public const string Bmp = "image/bmp";
    public const string Tiff = "image/tiff";
    public const string Svg = "image/svg+xml";

    //Office
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string Xls = "application/vnd.ms-excel";
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string Doc = "application/msword";
    public const string Pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    public const string Ppt = "application/vnd.ms-powerpoint";
    public const string Pdf = "application/pdf";
    public const string Csv = "text/csv";
    public const string Zip = "application/zip";
    public const string MsZipUpload = "application/x-zip-compressed";

    //Media
    public const string TransportStream = "video/mp2t";
    public const string Mpeg = "video/mpeg";
    public const string Mp4 = "video/mp4";
    public const string Avi = "video/x-msvideo";
    public const string Mp3 = "audio/mpeg";
    public const string Aac = "audio/aac";

    //Web
    public const string Html = "text/html";
    public const string Text = "text/plain";
    public const string Js = "text/javascript";
    public const string AppJs = "application/javascript";
    public const string Css = "text/css";
    public const string Xhtml = "application/xhtml+xml";
    public const string Xml = "application/xml";
    public const string BinaryStream = "application/octet-stream";
}

/// <summary>
/// Common encoding types
/// </summary>
public static class EncodingTypes
{
    public const string Brotli = "br";
    public const string GZip = "gzip";
    public const string Deflate = "deflate";
}
