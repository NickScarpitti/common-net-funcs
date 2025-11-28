using CommonNetFuncs.Web.Api.OpenApiTransformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Web.Api.Tests.OpenApiTransformers;

public sealed class OpenApiTransformerTests
{
	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(1)]
	public async Task HeaderTransformer_AddsAcceptHeaderParameter(object? initialParamCount)
	{
		// Arrange
		HeaderTransformer transformer = new();
		OpenApiOperation operation = new()
		{
#pragma warning disable S3923
			Parameters = initialParamCount switch
			{
				null => null,
				0 => null,
				1 => null,
				_ => null
			}
#pragma warning restore S3923
		};
		if (initialParamCount is 1)
		{
			operation.Parameters ??= [];
			operation.Parameters.Add(new OpenApiParameter { Name = "X-Test", In = ParameterLocation.Header });
		}
		CancellationToken cancellationToken = CancellationToken.None;

		// Act
		await transformer.TransformAsync(operation, null!, cancellationToken);

		// Assert
		operation.Parameters.ShouldNotBeNull();
		operation.Parameters.ShouldContain(p => p.Name == "Accept" && p.In == ParameterLocation.Header);
		IOpenApiParameter? acceptParam = operation.Parameters.FirstOrDefault(p => p.Name == "Accept");
		acceptParam.ShouldNotBeNull();
		acceptParam!.Required.ShouldBeTrue();
		acceptParam.Schema.ShouldNotBeNull();
		acceptParam.Schema.Type.ShouldBe(JsonSchemaType.String);
		acceptParam.Schema.Default.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("Bearer")]
	[InlineData("Other")]
	public async Task BearerSecuritySchemeTransformer_TransformAsync_AddsSecurityScheme_WhenAppropriate(string? schemeName)
	{
		// Arrange
		IAuthenticationSchemeProvider fakeProvider = A.Fake<IAuthenticationSchemeProvider>();
		List<AuthenticationScheme> schemes = new()
				{
						new AuthenticationScheme("Bearer", "Bearer", typeof(IAuthenticationHandler)),
						new AuthenticationScheme("Other", "Other", typeof(IAuthenticationHandler))
				};
		A.CallTo(() => fakeProvider.GetAllSchemesAsync()).Returns(Task.FromResult<IEnumerable<AuthenticationScheme>>(schemes));
		IOptions<BearerSecuritySchemeOptions> options = Options.Create(new BearerSecuritySchemeOptions { AuthenticationSchemeName = schemeName ?? "Bearer" });
		BearerSecuritySchemeTransformer transformer = new(fakeProvider, options);
		OpenApiDocument document = new();
		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "TestDoc",
			DescriptionGroups = [],
			ApplicationServices = null!
		};
		CancellationToken cancellationToken = CancellationToken.None;

		// Act
		await transformer.TransformAsync(document, context, cancellationToken);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components!.SecuritySchemes!.ShouldContainKey("Bearer");
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes!["Bearer"];
		scheme.Type.ShouldBe(SecuritySchemeType.Http);
		scheme.Scheme.ShouldBe("bearer");
		scheme.BearerFormat.ShouldBe("Json Web Token");
	}

	[Theory]
	[InlineData("NonExistentScheme")]
	public async Task BearerSecuritySchemeTransformer_TransformAsync_DoesNotAdd_WhenNoMatchingScheme(string schemeName)
	{
		// Arrange
		IAuthenticationSchemeProvider fakeProvider = A.Fake<IAuthenticationSchemeProvider>();
		List<AuthenticationScheme> schemes = new()
			{
				new AuthenticationScheme("Bearer", "Bearer", typeof(IAuthenticationHandler)),
				new AuthenticationScheme("Other", "Other", typeof(IAuthenticationHandler))
			};
		A.CallTo(() => fakeProvider.GetAllSchemesAsync()).Returns(Task.FromResult<IEnumerable<AuthenticationScheme>>(schemes));
		IOptions<BearerSecuritySchemeOptions> options = Options.Create(new BearerSecuritySchemeOptions { AuthenticationSchemeName = schemeName });
		BearerSecuritySchemeTransformer transformer = new(fakeProvider, options);
		OpenApiDocument document = new();
		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "TestDoc",
			DescriptionGroups = [],
			ApplicationServices = null!
		};
		CancellationToken cancellationToken = CancellationToken.None;

		// Act
		await transformer.TransformAsync(document, context, cancellationToken);

		// Assert
		document.Components.ShouldBeNull();
	}

	[Fact]
	public async Task BearerSecuritySchemeTransformer_TransformAsync_DoesNotAdd_WhenNoSchemes()
	{
		// Arrange
		IAuthenticationSchemeProvider fakeProvider = A.Fake<IAuthenticationSchemeProvider>();
		A.CallTo(() => fakeProvider.GetAllSchemesAsync()).Returns(Task.FromResult<IEnumerable<AuthenticationScheme>>(new List<AuthenticationScheme>()));
		IOptions<BearerSecuritySchemeOptions> options = Options.Create(new BearerSecuritySchemeOptions { AuthenticationSchemeName = "Bearer" });
		BearerSecuritySchemeTransformer transformer = new(fakeProvider, options);
		OpenApiDocument document = new();
		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "TestDoc",
			DescriptionGroups = [],
			ApplicationServices = null!
		};
		CancellationToken cancellationToken = CancellationToken.None;

		// Act
		await transformer.TransformAsync(document, context, cancellationToken);

		// Assert
		document.Components.ShouldBeNull();
	}
}
