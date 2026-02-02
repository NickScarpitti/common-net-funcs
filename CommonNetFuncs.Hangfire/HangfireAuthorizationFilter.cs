using System.Collections.Frozen;
using Hangfire.Dashboard;

namespace CommonNetFuncs.Hangfire;

/// <summary>
/// Authorization filter for Hangfire Dashboard - requires authenticated user with specific roles to access
/// </summary>
/// <param name="allowedRoles">Set of roles that are authorized to access the Hangfire Dashboard at http://<hostname>/hangfire</param>
public sealed class HangfireAuthorizationFilter(FrozenSet<string> allowedRoles) : IDashboardAuthorizationFilter
{
	/// <summary>
	/// Roles that are allowed to access the Hangfire Dashboard
	/// </summary>

	public bool Authorize(DashboardContext context)
	{
		Microsoft.AspNetCore.Http.HttpContext httpContext = context.GetHttpContext();

		if (httpContext?.User?.Identity?.IsAuthenticated != true)
		{
			return false;
		}

		// Check if user has any of the allowed roles
		if (allowedRoles.Count > 0)
		{
			return allowedRoles.Any(httpContext.User.IsInRole);
		}
		return true; // No specific roles required, just authentication
	}
}
