using MemoryPack;
using MessagePack;

namespace CommonNetFuncs.Web.Jwt;

// MemoryPack's generated serializer relies on static abstract interface members, which don't work correctly when compiled for netstandard2.1.
#if NET7_0_OR_GREATER
[MemoryPackable]
#endif
[MessagePackObject(true)]
public partial class JwtToken
{
	public string? Token { get; set; }

	public string? RefreshToken { get; set; }

	public DateTime? JwtExpireTime { get; set; }
}
