using MemoryPack;
using MessagePack;

namespace CommonNetFuncs.Web.Jwt;

[MemoryPackable]
[MessagePackObject(true)]
public partial class JwtToken
{
	public string? Token { get; set; }

	public string? RefreshToken { get; set; }

	public DateTime? JwtExpireTime { get; set; }
}
