using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace CommonNetFuncs.Web.Api.OpenApiTransformers;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider, IOptions<BearerSecuritySchemeOptions> options) : IOpenApiDocumentTransformer
{
	private readonly IAuthenticationSchemeProvider authenticationSchemeProvider = authenticationSchemeProvider;
	private readonly string authenticationSchemeName = options.Value.AuthenticationSchemeName;

	public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		IEnumerable<AuthenticationScheme> authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

		if (authenticationSchemeName == null || authenticationSchemes.Any(authScheme => authScheme.Name == authenticationSchemeName))
		{
			document.Components ??= new OpenApiComponents();

			const string securitySchemeId = "Bearer";

			var securityScheme = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "Json Web Token"
			};

			var requirements = new Dictionary<string, IOpenApiSecurityScheme>
			{
				[securitySchemeId] = securityScheme
			};

			document.Components.SecuritySchemes = requirements;
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
