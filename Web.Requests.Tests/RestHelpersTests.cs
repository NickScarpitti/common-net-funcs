using System.Reflection;
using CommonNetFuncs.Web.Requests.Rest;

namespace Web.Requests.Tests;

public sealed class RestHelpersTests
{
	[Fact]
	public void RestHelpers_InitializesHttpClient()
	{
		Type type = typeof(RestHelpers);
		FieldInfo? clientField = type.GetField("client", BindingFlags.Static | BindingFlags.NonPublic);
		object? value = clientField?.GetValue(null);

		value.ShouldBeOfType<HttpClient>();
		((HttpClient)value!).Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
	}

	[Fact]
	public void RestHelpers_CanBeInstantiated()
	{
		// Act
		RestHelpers restHelpers = new();

		// Assert
		restHelpers.ShouldNotBeNull();
		restHelpers.ShouldBeOfType<RestHelpers>();
		restHelpers.client.ShouldNotBeNull();
		restHelpers.client.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
	}

	[Fact]
	public void RestHelpers_UsesConfiguredSocketsHttpHandler()
	{
		// Arrange
		Type type = typeof(RestHelpers);
		FieldInfo? handlerField = type.GetField("socketsHttpHandler", BindingFlags.Static | BindingFlags.NonPublic);
		object? handlerValue = handlerField?.GetValue(null);

		// Assert
		handlerValue.ShouldBeOfType<SocketsHttpHandler>();
		SocketsHttpHandler handler = (SocketsHttpHandler)handlerValue!;
		handler.MaxConnectionsPerServer.ShouldBe(100);
		handler.KeepAlivePingPolicy.ShouldBe(HttpKeepAlivePingPolicy.Always);
		handler.KeepAlivePingDelay.ShouldBe(TimeSpan.FromSeconds(15));
		handler.KeepAlivePingTimeout.ShouldBe(TimeSpan.FromMinutes(60));
	}
}
