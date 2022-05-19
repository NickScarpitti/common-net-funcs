using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommonNetCoreFuncs.Tools;
using Microsoft.IdentityModel.Tokens;

namespace CommonNetCoreFuncs.Web.JWT;

public class JWTManager : IJWTManager
{
	public TokenObject? Authenticate(string? userName, string? password, string actualUserName, string actualPassword, string environment, string key, TimeSpan devTokenLifespan, TimeSpan stdTokenLifespan)
	{
		if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || userName != actualUserName || password != actualPassword)
		{
			return null;
		}

		//Generate JSON Web Token if valid request
		var tokenHandler = new JwtSecurityTokenHandler();
		var tokenKey = Encoding.UTF8.GetBytes(key);
		SecurityTokenDescriptor tokenDescriptor = new()
		{
			Subject = new ClaimsIdentity(new Claim[]
			{
				new Claim(ClaimTypes.Name, userName)
			}),
			IssuedAt = DateTime.UtcNow,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
		};

        if (!environment.StrEq("Development"))
        {
			tokenDescriptor.Expires = DateTime.UtcNow.Add(stdTokenLifespan);
        }
        else
        {
			tokenDescriptor.Expires = DateTime.UtcNow.Add(devTokenLifespan);
		}

		SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

		TokenObject returnToken = new() { Token = tokenHandler.WriteToken(token), JwtExpireTime = tokenDescriptor.Expires };

		return returnToken;
	}
}