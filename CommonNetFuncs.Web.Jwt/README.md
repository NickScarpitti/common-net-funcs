# CommonNetFuncs.Web.Jwt

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Web.Jwt)](https://www.nuget.org/packages/CommonNetFuncs.Web.Jwt/)

This lightweight project contains helper methods for several common functions required by applications.

## Contents

- [CommonNetFuncs.Web.Jwt](#commonnetfuncswebjwt)
  - [Contents](#contents)
  - [JwtManager](#jwtmanager)
    - [JwtManager Usage Examples](#jwtmanager-usage-examples)
      - [Authenticate](#authenticate)

---

## JwtManager

Provides an authentication mechanism that issues JWT tokens when the correct credentials are provided.

### JwtManager Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### Authenticate

Authenticate credentials and issue a JWT token if credentials are valid.

```cs
public class JwtToken // This is a class present within the CommonNetFuncs.Web.Jwt package
{
  public string? Token { get; set; }
  public string? RefreshToken { get; set; }
  public DateTime? JwtExpireTime { get; set; }
}

JwtToken? jwtToken = Authenticate("user-name", "password", "user-name-secret", "password-secret", "production", "key-secret", TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5), "issuer", "email", "audience");
// All "-secret" values should be secrets in the host application and are validated against the user submitted values.
// Issuer, Email, and Audience parameters are all optional depending on the info you want to include in the JWT token
```

</details>
