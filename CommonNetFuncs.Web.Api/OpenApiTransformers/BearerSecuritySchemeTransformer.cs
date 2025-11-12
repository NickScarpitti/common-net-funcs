using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace CommonNetFuncs.Web.Api.OpenApiTransformers;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider, IOptions<BearerSecuritySchemeOptions> options) : IOpenApiDocumentTransformer
{
	private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider = authenticationSchemeProvider;
	private readonly string? _authenticationSchemeName = options.Value.AuthenticationSchemeName;

	public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		IEnumerable<AuthenticationScheme> authenticationSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

		if (_authenticationSchemeName == null || authenticationSchemes.Any(authScheme => authScheme.Name == _authenticationSchemeName))
		{
			document.Components ??= new OpenApiComponents();
			document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

			const string securitySchemeId = "Bearer";

			document.Components.SecuritySchemes[securitySchemeId] = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
				In = ParameterLocation.Header,
				Description = "JWT Authorization header using the Bearer scheme."
			};

			document.Security ??= new List<OpenApiSecurityRequirement>();
			document.Security.Add(new OpenApiSecurityRequirement
			{
				{ new OpenApiSecuritySchemeReference(securitySchemeId), new List<string>() }
			});
		}
	}
}

/// <summary>
/// Name of the authentication scheme to look for when deciding whether to add the Bearer security scheme to the OpenAPI document.
/// </summary>
public class BearerSecuritySchemeOptions
{
	public string AuthenticationSchemeName { get; set; } = "Bearer";
}
