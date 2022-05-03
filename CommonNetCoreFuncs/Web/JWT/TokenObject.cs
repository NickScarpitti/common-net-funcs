using System;

namespace CommonNetCoreFuncs.Web.JWT;

public class TokenObject
{
	public string? Token { get; set; }
	public string? RefreshToken { get; set; }
    public DateTime? JwtExpireTime { get; set; }
}
