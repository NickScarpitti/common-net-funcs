using static CommonNetFuncs.Compression.Streams;

namespace CommonNetFuncs.Web.Requests.Rest.Options;

public sealed class CompressionOptions(bool UseCompression = false, ECompressionType? CompressionType = null, bool UseMemPack = false, bool UseMsgPack = false)
{
	public bool UseCompression { get; set; } = UseCompression;

	public ECompressionType? CompressionType { get; set; } = CompressionType;

	public bool UseMemPack { get; set; } = UseMemPack;

	public bool UseMsgPack { get; set; } = UseMsgPack;
}
