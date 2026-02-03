using System.Collections.Frozen;
using System.Security.Claims;
using CommonNetFuncs.Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Tests;

public sealed class HangfireAuthorizationFilterTests
{
	private static AspNetCoreDashboardContext CreateDashboardContext(HttpContext httpContext)
	{
		JobStorage storage = A.Fake<JobStorage>();
		DashboardOptions options = new();

		// Setup HttpContext with RequestServices if not set
		if (httpContext.RequestServices == null)
		{
			ServiceCollection services = new();
			httpContext.RequestServices = services.BuildServiceProvider();
		}

		return new AspNetCoreDashboardContext(storage, options, httpContext);
	}

	[Fact]
	public void Constructor_ShouldInitializeWithAllowedRoles()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin", "Manager" }.ToFrozenSet();

		// Act
		HangfireAuthorizationFilter filter = new(allowedRoles);

		// Assert
		Assert.NotNull(filter);
	}

	[Fact]
	public void Authorize_ShouldReturnFalse_WhenUserIsNull()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = null!
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void Authorize_ShouldReturnFalse_WhenUserIdentityIsNull()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsPrincipal user = new();
		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void Authorize_ShouldReturnFalse_WhenUserIsNotAuthenticated()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new();
		ClaimsPrincipal user = new(identity);
		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.False(result);
	}

	[Theory]
	[InlineData("Admin")]
	[InlineData("Manager")]
	[InlineData("SuperUser")]
	public void Authorize_ShouldReturnTrue_WhenUserHasAllowedRole(string userRole)
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin", "Manager", "SuperUser" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new(
			[new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.Role, userRole)],
			"TestAuthType");
		ClaimsPrincipal user = new(identity);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.True(result);
	}

	[Theory]
	[InlineData("User")]
	[InlineData("Guest")]
	[InlineData("Anonymous")]
	public void Authorize_ShouldReturnFalse_WhenUserDoesNotHaveAllowedRole(string userRole)
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin", "Manager" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new(
			[new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.Role, userRole)],
			"TestAuthType");
		ClaimsPrincipal user = new(identity);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void Authorize_ShouldReturnTrue_WhenUserHasMultipleRoles_IncludingAllowedOne()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new(
			[
				new Claim(ClaimTypes.Name, "TestUser"),
				new Claim(ClaimTypes.Role, "User"),
				new Claim(ClaimTypes.Role, "Admin"),
				new Claim(ClaimTypes.Role, "Guest")
			],
			"TestAuthType");
		ClaimsPrincipal user = new(identity);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void Authorize_ShouldReturnFalse_WhenUserHasNoRoles()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new(
			[new Claim(ClaimTypes.Name, "TestUser")],
			"TestAuthType");
		ClaimsPrincipal user = new(identity);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void Authorize_ShouldReturnTrue_WhenAllowedRolesIsEmpty_AndUserIsAuthenticated()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string>().ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new(
			[new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.Role, "Admin")],
			"TestAuthType");
		ClaimsPrincipal user = new(identity);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert - When no roles are specified, any authenticated user should be allowed
		Assert.True(result);
	}

	[Fact]
	public void Authorize_ShouldBeCaseSensitive_ForRoleNames()
	{
		// Arrange
		FrozenSet<string> allowedRoles = new HashSet<string> { "Admin" }.ToFrozenSet();
		HangfireAuthorizationFilter filter = new(allowedRoles);

		ClaimsIdentity identity = new(
			[new Claim(ClaimTypes.Name, "TestUser"), new Claim(ClaimTypes.Role, "admin")], // lowercase
			"TestAuthType");
		ClaimsPrincipal user = new(identity);

		HttpContext httpContext = new DefaultHttpContext
		{
			User = user
		};

		DashboardContext dashboardContext = CreateDashboardContext(httpContext);

		// Act
		bool result = filter.Authorize(dashboardContext);

		// Assert
		Assert.False(result);
	}
}
