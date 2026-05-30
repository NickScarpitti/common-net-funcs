using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CommonNetFuncs.Web.Api;

/// <summary>
/// Endpoint filter that validates request body arguments decorated with DataAnnotations attributes, returning a 400 ValidationProblem response when validation fails.
/// This is identical to MVC model validation behavior.
/// Apply globally via <see cref="ValidationEndpointFilterExtensions.WithValidation{TBuilder}"/> on a route group to avoid per-endpoint or per-model configuration.
/// </summary>
public sealed class ValidationEndpointFilter(IServiceProviderIsService serviceChecker) : IEndpointFilter
{
	// Framework-injected types that must never be passed to Validator.TryValidateObject
	private static readonly FrozenSet<Type> SkippedTypes =
	[
		typeof(CancellationToken),
		typeof(HttpContext),
		typeof(HttpRequest),
		typeof(HttpResponse),
		typeof(ClaimsPrincipal),
		typeof(IServiceProvider),
		typeof(LinkGenerator),
	];

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		foreach (object? argument in context.Arguments)
		{
			if (argument is null) continue;

			Type argType = argument.GetType();

			if (!ShouldValidate(argType)) continue;

			List<ValidationResult> validationResults = [];
			ValidationContext validationContext = new(argument, context.HttpContext.RequestServices, items: null);

			if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
			{
				Dictionary<string, string[]> errors = validationResults
					.GroupBy(vr => vr.MemberNames.FirstOrDefault() ?? string.Empty)
					.ToDictionary(x => x.Key, x => x.Select(vr => vr.ErrorMessage ?? string.Empty).ToArray());

				return TypedResults.ValidationProblem(errors);
			}
		}

		return await next(context);
	}

	private bool ShouldValidate(Type type)
	{
		// Primitive value types and their common equivalents carry no validation attributes
		if (type.IsPrimitive || type.IsEnum) return false;
		if (type == typeof(string) || type == typeof(decimal) || type == typeof(Guid)) return false;
		if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)) return false;

		// Explicitly skipped well-known framework types
		if (SkippedTypes.Contains(type)) return false;

		// Catch any HttpContext / ClaimsPrincipal subclasses not listed above
		if (type.IsAssignableTo(typeof(HttpContext)) || type.IsAssignableTo(typeof(ClaimsPrincipal))) return false;

		// Skip any type registered in the DI container (covers all app services, ILogger<TNumber>, etc.)
		if (serviceChecker.IsService(type)) return false;

		return true;
	}
}

public static class ValidationEndpointFilterExtensions
{
	/// <summary>
	/// Adds automatic DataAnnotations validation to every endpoint registered on this builder.
	/// A 400 ValidationProblem is returned for any invalid bound model — no per-endpoint or per-model wiring required.
	/// </summary>
	public static RouteGroupBuilder WithValidation(this RouteGroupBuilder builder)
		=> builder.AddEndpointFilter<ValidationEndpointFilter>();

	/// <inheritdoc cref="WithValidation(RouteGroupBuilder)"/>
	public static RouteHandlerBuilder WithValidation(this RouteHandlerBuilder builder)
		=> builder.AddEndpointFilter<ValidationEndpointFilter>();
}
