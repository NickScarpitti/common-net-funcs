using MemoryPack;
using MessagePack;

namespace Common_Net_Funcs.Web.JWT;

[MemoryPackable]
[MessagePackObject(true)]
public partial class JwtToken
{
	public string? Token { get; set; }
	public string? RefreshToken { get; set; }
    public DateTime? JwtExpireTime { get; set; }
}
