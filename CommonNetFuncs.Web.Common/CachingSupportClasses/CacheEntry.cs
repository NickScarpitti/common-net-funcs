namespace CommonNetFuncs.Web.Middleware;

public class CacheEntry
{
    public byte[] Data { get; set; } = [];
    public HashSet<string> Tags { get; set; } = [];
    public Dictionary<string, string> Headers { get; set; } = [];
    public short CompressionType { get; set; }
}
