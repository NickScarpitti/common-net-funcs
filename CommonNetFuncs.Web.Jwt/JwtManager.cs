using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CommonNetFuncs.Web.Jwt;

public interface IJwtManager
{
  JwtToken? Authenticate(string? userName, string? password, string actualUserName, string actualPassword, string environment, string key, TimeSpan devTokenLifespan,
		TimeSpan stdTokenLifespan, string? issuer = null, string? email = null, string? audience = null);
}

/// <summary>
/// Helper functions for use when implementing JWT Bearer Authentication
/// </summary>
public sealed class JwtManager : IJwtManager
{
  /// <summary>
  /// Authenticates a JWT token request
  /// </summary>
  /// <param name="userName">Passed in user name</param>
  /// <param name="password">Passed in password</param>
  /// <param name="actualUserName">Actual user name from application secret</param>
  /// <param name="actualPassword">Actual password from application secret</param>
  /// <param name="environment">Current environment name</param>
  /// <param name="key">Application token key from application secret</param>
  /// <param name="devTokenLifespan">How long the token should remain valid in development environment</param>
  /// <param name="stdTokenLifespan">How long the token should remain valid in a non-development environment</param>
  /// <returns>JwtToken if the credentials passed in are valid></returns>
  public JwtToken? Authenticate(string? userName, string? password, string actualUserName, string actualPassword, string environment, string key, TimeSpan devTokenLifespan,
		TimeSpan stdTokenLifespan, string? issuer = null, string? email = null, string? audience = null)
  {
    if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || !string.Equals(userName, actualUserName) || !string.Equals(password, actualPassword))
    {
      return null;
    }

    JsonWebTokenHandler tokenHandler = new();

    SecurityTokenDescriptor tokenDescriptor = new()
		{
			Subject = new ClaimsIdentity([ new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.Email, email ?? string.Empty) ]),
			IssuedAt = DateTime.UtcNow,
			SigningCredentials = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha512Signature, SecurityAlgorithms.Sha512Digest)
		};

    if (!string.Equals(environment, "Development", StringComparison.InvariantCultureIgnoreCase))
    {
      tokenDescriptor.Expires = DateTime.UtcNow.Add(stdTokenLifespan);
    }
    else
    {
      tokenDescriptor.Expires = DateTime.UtcNow.Add(devTokenLifespan);
    }

    if (!string.IsNullOrWhiteSpace(issuer))
    {
      tokenDescriptor.Issuer = issuer;
    }

    if (!string.IsNullOrWhiteSpace(audience))
    {
      tokenDescriptor.Audience = audience;
    }

    return new() { Token = tokenHandler.CreateToken(tokenDescriptor), JwtExpireTime = tokenDescriptor.Expires };
  }
}
