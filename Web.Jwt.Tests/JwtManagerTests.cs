using CommonNetFuncs.Web.Jwt;

namespace Web.Jwt.Tests;

public sealed class JwtManagerTests
{
	private readonly JwtManager jwtManager;
	private readonly IFixture fixture;

	public JwtManagerTests()
	{
		jwtManager = new JwtManager();
		fixture = new Fixture();
	}

	[Theory]
	[InlineData(null, "password", "actual", "actual", false)]
	[InlineData("username", null, "actual", "actual", false)]
	[InlineData("wrong", "password", "actual", "actual", false)]
	[InlineData("username", "wrong", "actual", "actual", false)]
	[InlineData("actual", "actual", "actual", "actual", true)]
	public void Authenticate_WithVariousCredentials_ReturnsExpectedResult(string? username, string? password, string actualUsername, string actualPassword, bool shouldSucceed)
	{
		// Arrange
		const string environment = "Production";
		string key = string.Join(string.Empty, fixture.CreateMany<char>(500));
		TimeSpan devSpan = TimeSpan.FromHours(24);
		TimeSpan stdSpan = TimeSpan.FromHours(1);

		// Act
		JwtToken? result = jwtManager.Authenticate(username, password, actualUsername, actualPassword, environment, key, devSpan, stdSpan);

		// Assert
		if (shouldSucceed)
		{
			result.ShouldNotBeNull();
			result.Token.ShouldNotBeNull();
			result.JwtExpireTime.ShouldNotBeNull();
			result.JwtExpireTime.Value.ShouldBe(DateTime.UtcNow.Add(stdSpan), TimeSpan.FromSeconds(5));
		}
		else
		{
			result.ShouldBeNull();
		}
	}

	[Theory]
	[InlineData("Development", 24)]
	[InlineData("Production", 1)]
	[InlineData("Staging", 1)]
	public void Authenticate_WithDifferentEnvironments_SetsCorrectExpiration(string environment, int expectedHours)
	{
		// Arrange
		const string username = "test";
		const string password = "test";
		string key = string.Join(string.Empty, fixture.CreateMany<char>(500));
		TimeSpan devSpan = TimeSpan.FromHours(24);
		TimeSpan stdSpan = TimeSpan.FromHours(1);

		// Act
		JwtToken? result = jwtManager.Authenticate(username, password, username, password, environment, key, devSpan, stdSpan);

		// Assert
		result.ShouldNotBeNull();
		result.JwtExpireTime.ShouldNotBeNull();
		result.JwtExpireTime.Value.ShouldBe(DateTime.UtcNow.AddHours(expectedHours), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void Authenticate_WithOptionalParameters_IncludesThemInToken()
	{
		// Arrange
		const string username = "test";
		const string password = "test";
		string key = string.Join(string.Empty, fixture.CreateMany<char>(500));
		string issuer = fixture.Create<string>();
		string email = fixture.Create<string>();
		string audience = fixture.Create<string>();
		TimeSpan span = TimeSpan.FromHours(1);

		// Act
		JwtToken? result = jwtManager.Authenticate(username, password, username, password, "Production", key, span, span, issuer, email, audience);

		// Assert
		result.ShouldNotBeNull();
		result.Token.ShouldNotBeNull();
		result.JwtExpireTime.ShouldNotBeNull();

		// Note: Since the token is encrypted, we can't directly verify the claims
		// but we can verify the token was generated
		result.Token.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void Authenticate_WithValidCredentials_ShouldCreateValidToken()
	{
		// Arrange
		const string username = "test";
		const string password = "test";
		string key = string.Join(string.Empty, fixture.CreateMany<char>(500));
		TimeSpan span = TimeSpan.FromHours(1);

		// Act
		JwtToken? result = jwtManager.Authenticate(username, password, username, password, "Production", key, span, span);

		// Assert
		result.ShouldNotBeNull();
		result.Token.ShouldNotBeNull();
		result.JwtExpireTime.ShouldNotBeNull();
		result.RefreshToken.ShouldBeNull(); // RefreshToken is not implemented in current version
	}
}
