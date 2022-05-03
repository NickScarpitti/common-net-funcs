using System;
namespace CommonNetCoreFuncs.Web.JWT;

public interface IJWTManager
{
    TokenObject? Authenticate(string? userName, string? password, string actualUserName, string actualPassword, string environment, string key, TimeSpan devTokenLifespan, TimeSpan stdTokenLifespan);
}
