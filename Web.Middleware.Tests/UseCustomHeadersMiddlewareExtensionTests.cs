using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class UseCustomHeadersMiddlewareExtensionTests
{
	private readonly IApplicationBuilder _builder;

	public UseCustomHeadersMiddlewareExtensionTests()
	{
		_builder = A.Fake<IApplicationBuilder>();
		// Setup UseMiddleware to return the builder for fluent chaining
		A.CallTo(() => _builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(_builder);
	}

	[RetryFact(3)]
	public void UseCustomHeaders_WithParameters_ReturnsBuilder()
	{
		// Arrange
		Dictionary<string, string> addHeaders = new() { { "X-Test", "Value" } };
		string[] removeHeaders = new[] { "X-Remove" };

		// Act
		IApplicationBuilder result = _builder.UseCustomHeaders(addHeaders, removeHeaders);

		// Assert
		result.ShouldBe(_builder);
	}

	[RetryFact(3)]
	public void UseCustomHeaders_WithNoParameters_ReturnsBuilder()
	{
		// Act
		IApplicationBuilder result = _builder.UseCustomHeaders();

		// Assert
		result.ShouldBe(_builder);
	}
}
