using CommonNetFuncs.Web.Api.OpenApiTransformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

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
			Parameters = initialParamCount switch
			{
				null => null,
				0 => new List<OpenApiParameter>(),
				1 => new List<OpenApiParameter> { new() { Name = "X-Test", In = ParameterLocation.Header } },
				_ => null
			}
		};
		CancellationToken cancellationToken = CancellationToken.None;

		// Act
		await transformer.TransformAsync(operation, null!, cancellationToken);

		// Assert
		operation.Parameters.ShouldNotBeNull();
		operation.Parameters.ShouldContain(p => p.Name == "Accept" && p.In == ParameterLocation.Header);
		OpenApiParameter? acceptParam = operation.Parameters.FirstOrDefault(p => p.Name == "Accept");
		acceptParam.ShouldNotBeNull();
		acceptParam!.Required.ShouldBeTrue();
		acceptParam.Schema.ShouldNotBeNull();
		acceptParam.Schema.Type.ShouldBe("string");
		acceptParam.Schema.Default.ShouldBeOfType<OpenApiString>();
		((OpenApiString)acceptParam.Schema.Default).Value.ShouldBe("application/json");
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
		document.Components.SecuritySchemes.ShouldContainKey("Bearer");
		OpenApiSecurityScheme scheme = document.Components.SecuritySchemes["Bearer"];
		scheme.Type.ShouldBe(SecuritySchemeType.Http);
		scheme.Scheme.ShouldBe("bearer");
		scheme.In.ShouldBe(ParameterLocation.Header);
		scheme.BearerFormat.ShouldBe("Json Web Token");
		document.SecurityRequirements.ShouldNotBeEmpty();
		document.SecurityRequirements.Any(req => req.Keys.Any(k => k.Reference?.Id == "Bearer" && k.Reference.Type == ReferenceType.SecurityScheme)).ShouldBeTrue();
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
		document.SecurityRequirements.ShouldBeEmpty();
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
		document.SecurityRequirements.ShouldBeEmpty();
	}
}