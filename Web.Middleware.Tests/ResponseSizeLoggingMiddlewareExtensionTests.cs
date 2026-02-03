using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class ResponseSizeLoggingMiddlewareExtensionTests
{
	private readonly IApplicationBuilder _builder;

	public ResponseSizeLoggingMiddlewareExtensionTests()
	{
		_builder = A.Fake<IApplicationBuilder>();
		// Setup UseMiddleware to return the builder for fluent chaining
		A.CallTo(() => _builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(_builder);
	}

	[RetryFact(3)]
	public void UseResponseSizeLogging_RegistersMiddleware()
	{
		// Act
		IApplicationBuilder result = _builder.UseResponseSizeLogging();

		// Assert
		result.ShouldBe(_builder);
	}
}
