namespace CommonNetFuncs.Web.Common;

/// <summary>
/// Common content types
/// </summary>
public static class ContentTypes
{
    /// <summary>
    /// Determines the MIME content type based on the file extension of the specified file name.
    /// </summary>
    /// <remarks>This method uses the file extension to identify the MIME type. Ensure the file name includes a valid extension.</remarks>
    /// <param name="fileName">The name of the file, including its extension. Cannot be null or empty.</param>
    /// <returns>A string representing the MIME content type associated with the file extension. Returns an empty string if the file extension is not recognized.</returns>
    public static string GetContentType(this string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }
        return GetContentTypeByExtension(Path.GetExtension(fileName));
    }

    public static string GetContentTypeByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            throw new ArgumentException("File extension cannot be null or empty.", nameof(extension));
        }
        // Ensure the extension starts with a dot
        if (!extension.StartsWith('.'))
        {
            extension = $".{extension}";
        }

        // Use switch expression to return content type based on file extension
        return extension.ToLowerInvariant() switch
        {
            ".json" => Json,
            ".mempack" => MemPack,
            ".msgpack" => MsgPack,
            ".png" => Png,
            ".jpg" or ".jpeg" => Jpeg,
            ".gif" => Gif,
            ".bmp" => Bmp,
            ".tiff" => Tiff,
            ".svg" => Svg,
            ".xlsx" => Xlsx,
            ".xls" => Xls,
            ".docx" => Docx,
            ".doc" => Doc,
            ".pptx" => Pptx,
            ".ppt" => Ppt,
            ".pdf" => Pdf,
            ".csv" => Csv,
            ".zip" or ".mszipupload" => Zip,
            ".ts" => TransportStream,
            ".mpeg" or ".mpg" => Mpeg,
            ".mp4" => Mp4,
            ".webm" => Webm,
            ".flv" => Flv,
            ".avi" => Avi,
            ".mp3" => Mp3,
            ".aac" => Aac,
            _ when FormDataTypes.Contains(extension) => UrlEncodedFormData, // Default to form data types
            _ when extension.StartsWith(".html") || extension.StartsWith(".htm") => Html, // Handle HTML files
            _ when extension.StartsWith(".txt") || extension.StartsWith(".text") => Text, // Handle text files
            _ when extension.StartsWith(".js") || extension.StartsWith(".javascript") => Js, // Handle JavaScript files
            _ when extension.StartsWith(".css") || extension.StartsWith(".stylesheet") => Css, // Handle CSS files
            _ when extension.StartsWith(".xml") || extension.StartsWith(".xhtml") => Xml, // Handle XML files
            _ when extension.StartsWith(".binary") || extension.StartsWith(".octet-stream") => BinaryStream, // Handle binary streams
            _ => "application/octet-stream", // Default content type for unknown extensions
        };
    }

    public const string Json = "application/json";
    public const string JsonProblem = "application/problem+json";
    public const string MemPack = "application/x-memorypack";
    public const string MsgPack = "application/x-msgpack";
    public const string UrlEncodedFormData = "application/x-www-form-urlencoded";
    public const string MultiPartFormData = "multipart/form-data";
    public static readonly string[] FormDataTypes = [UrlEncodedFormData, MultiPartFormData];

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
    public const string Webm = "video/webm";
    public const string Flv = "video/x-flv";
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
    public const string Identity = "identity";
    public const string Brotli = "br";
    public const string GZip = "gzip";
    public const string Deflate = "deflate";
}
