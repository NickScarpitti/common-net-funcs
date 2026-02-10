using CommonNetFuncs.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using xRetry.v3;

namespace Web.Middleware.Tests;

public sealed class ResponseSizeLoggingMiddlewareExtensionTests
{
	private readonly IApplicationBuilder builder;

	public ResponseSizeLoggingMiddlewareExtensionTests()
	{
		builder = A.Fake<IApplicationBuilder>();
		// Setup UseMiddleware to return the builder for fluent chaining
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);
	}

	[RetryFact(3)]
	public void UseResponseSizeLogging_RegistersMiddleware()
	{
		// Act
		IApplicationBuilder result = builder.UseResponseSizeLogging();

		// Assert
		result.ShouldBe(builder);
	}
}
