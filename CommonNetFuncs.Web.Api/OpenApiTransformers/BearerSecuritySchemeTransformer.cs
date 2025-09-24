using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

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

      document.Components.SecuritySchemes.Add(securitySchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token"
            });

      // Add "Bearer" scheme as a requirement for the API as a whole
      document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = securitySchemeId, Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
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
