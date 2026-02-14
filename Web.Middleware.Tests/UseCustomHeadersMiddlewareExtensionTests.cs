using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class UseCustomHeadersMiddlewareExtensionTests
{
	private readonly IApplicationBuilder builder;

	public UseCustomHeadersMiddlewareExtensionTests()
	{
		builder = A.Fake<IApplicationBuilder>();
		// Setup UseMiddleware to return the builder for fluent chaining
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);
	}

	[RetryFact(3)]
	public void UseCustomHeaders_WithParameters_ReturnsBuilder()
	{
		// Arrange
		Dictionary<string, string> addHeaders = new() { { "X-Test", "Value" } };
		string[] removeHeaders = new[] { "X-Remove" };

		// Act
		IApplicationBuilder result = builder.UseCustomHeaders(addHeaders, removeHeaders);

		// Assert
		result.ShouldBe(builder);
	}

	[RetryFact(3)]
	public void UseCustomHeaders_WithNoParameters_ReturnsBuilder()
	{
		// Act
		IApplicationBuilder result = builder.UseCustomHeaders();

		// Assert
		result.ShouldBe(builder);
	}
}
