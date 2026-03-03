using System.Net;
using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using static Xunit.TestContext;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public class EndpointQueueExtensionsTests
{
	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_Should_Invoke_Service()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		BoundedChannelOptions options = new(1);

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(123);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options);

		// Assert
		result.ShouldBe(123);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Unbounded_Should_Invoke_Service()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		UnboundedChannelOptions options = new();

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(456);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(456), options);

		// Assert
		result.ShouldBe(456);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_With_CustomKey_Should_Use_CustomKey()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		BoundedChannelOptions options = new(1);
		const string customKey = "CustomEndpointKey";
		string capturedKey = string.Empty;

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(123)
			.Callback<string, Func<CancellationToken, Task<int>>, BoundedChannelOptions, CancellationToken>((key, _, _, _) => capturedKey = key);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options, customKey);

		// Assert
		result.ShouldBe(123);
		capturedKey.ShouldBe(customKey);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Unbounded_With_CustomKey_Should_Use_CustomKey()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		UnboundedChannelOptions options = new();
		const string customKey = "CustomEndpointKeyUnbounded";
		string capturedKey = string.Empty;

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(456)
			.Callback<string, Func<CancellationToken, Task<int>>, UnboundedChannelOptions, CancellationToken>((key, _, _, _) => capturedKey = key);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(456), options, customKey);

		// Assert
		result.ShouldBe(456);
		capturedKey.ShouldBe(customKey);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_Without_CustomKey_Should_Generate_EndpointKey()
	{
		// Arrange
		TestController controller = new();
		Mock<IEndpointQueueService> serviceMock = new();
		BoundedChannelOptions options = new(1);
		string capturedKey = string.Empty;

		Mock<ControllerActionDescriptor> actionDescriptorMock = new();
		actionDescriptorMock.Object.ActionName = "TestAction";
		controller.ControllerContext = new ControllerContext
		{
			ActionDescriptor = actionDescriptorMock.Object
		};

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(123)
			.Callback<string, Func<CancellationToken, Task<int>>, BoundedChannelOptions, CancellationToken>((key, _, _, _) => capturedKey = key);

		// Act
		int result = await controller.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options);

		// Assert
		result.ShouldBe(123);
		// Verify that a key was generated (contains controller name)
		capturedKey.ShouldContain("Test");
		capturedKey.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Unbounded_Without_CustomKey_Should_Generate_EndpointKey()
	{
		// Arrange
		TestController controller = new();
		Mock<IEndpointQueueService> serviceMock = new();
		UnboundedChannelOptions options = new();
		string capturedKey = string.Empty;

		Mock<ControllerActionDescriptor> actionDescriptorMock = new();
		actionDescriptorMock.Object.ActionName = "AnotherAction";
		controller.ControllerContext = new ControllerContext
		{
			ActionDescriptor = actionDescriptorMock.Object
		};

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(456)
			.Callback<string, Func<CancellationToken, Task<int>>, UnboundedChannelOptions, CancellationToken>((key, _, _, _) => capturedKey = key);

		// Act
		int result = await controller.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(456), options);

		// Assert
		result.ShouldBe(456);
		// Verify that a key was generated (contains controller name)
		capturedKey.ShouldContain("Test");
		capturedKey.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_With_HttpContext_CancellationToken_Should_Pass_Token()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		Mock<HttpContext> httpContextMock = new();
		BoundedChannelOptions options = new(1);
		using CancellationTokenSource cts = new();
		CancellationToken capturedToken = default;

		httpContextMock.Setup(x => x.RequestAborted).Returns(cts.Token);
		controllerMock.Object.ControllerContext = new ControllerContext { HttpContext = httpContextMock.Object };

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(123)
			.Callback<string, Func<CancellationToken, Task<int>>, BoundedChannelOptions, CancellationToken>((_, _, _, token) => capturedToken = token);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options);

		// Assert
		result.ShouldBe(123);
		capturedToken.ShouldBe(cts.Token);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Unbounded_With_HttpContext_CancellationToken_Should_Pass_Token()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		Mock<HttpContext> httpContextMock = new();
		UnboundedChannelOptions options = new();
		using CancellationTokenSource cts = new();
		CancellationToken capturedToken = default;

		httpContextMock.Setup(x => x.RequestAborted).Returns(cts.Token);
		controllerMock.Object.ControllerContext = new ControllerContext { HttpContext = httpContextMock.Object };

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(456)
			.Callback<string, Func<CancellationToken, Task<int>>, UnboundedChannelOptions, CancellationToken>((_, _, _, token) => capturedToken = token);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(456), options);

		// Assert
		result.ShouldBe(456);
		capturedToken.ShouldBe(cts.Token);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_Without_ActionName_Should_Use_NoAction()
	{
		TestController controller = new();
		Mock<IEndpointQueueService> serviceMock = new();
		BoundedChannelOptions options = new(1);
		string capturedKey = string.Empty;

		controller.ControllerContext = new ControllerContext
		{
			ActionDescriptor = null!
		};

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(123)
			.Callback<string, Func<CancellationToken, Task<int>>, BoundedChannelOptions, CancellationToken>((key, _, _, _) => capturedKey = key);

		// Act
		int result = await controller.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options);

		// Assert
		result.ShouldBe(123);
		capturedKey.ShouldBe("Test.NoAction");
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_Without_HttpContext_Should_Use_Default_CancellationToken()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		BoundedChannelOptions options = new(1);
		CancellationToken capturedToken = CancellationToken.None;

		controllerMock.Object.ControllerContext = new ControllerContext { HttpContext = null! };

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync(123)
			.Callback<string, Func<CancellationToken, Task<int>>, BoundedChannelOptions, CancellationToken>((_, _, _, token) => capturedToken = token);

		// Act
		int result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult(123), options);

		// Assert
		result.ShouldBe(123);
		capturedToken.ShouldBe(default);
	}

	[Fact]
	public void EndpointQueueMetrics_Should_Register_GetAll_Endpoint()
	{
		// Arrange
		Mock<IEndpointRouteBuilder> endpointsMock = new();

		endpointsMock.Setup(x => x.CreateApplicationBuilder()).Returns(Mock.Of<IApplicationBuilder>());

		endpointsMock.Setup(x => x.DataSources).Returns(new List<EndpointDataSource>());

		endpointsMock.Setup(x => x.ServiceProvider).Returns(Mock.Of<IServiceProvider>());

		// Act - Call the extension method directly from EndpointQueueExtensions
		IEndpointRouteBuilder result = EndpointQueueExtensions.EndpointQueueMetrics(endpointsMock.Object);

		// Assert
		result.ShouldBe(endpointsMock.Object);
		// Verify that DataSources was accessed (which happens when endpoints are registered)
		endpointsMock.Verify(x => x.DataSources, Moq.Times.AtLeastOnce);
	}

	[Fact]
	public async Task EndpointQueueMetrics_GetAll_Should_Return_Stats()
	{
		// Arrange
		EndpointQueueService queueService = new();
		Dictionary<string, QueueStats> expectedStats = await queueService.GetAllQueueStatsAsync();

		// Act & Assert
		expectedStats.ShouldNotBeNull();
		expectedStats.ShouldBeOfType<Dictionary<string, QueueStats>>();
	}

	[Fact]
	public async Task EndpointQueueMetrics_GetByKey_Should_Return_Stats_For_Key()
	{
		// Arrange
		EndpointQueueService queueService = new();
		const string testKey = "TestEndpoint";

		// Execute a task to create the queue
		await queueService.ExecuteAsync(testKey, _ => Task.FromResult(42), new BoundedChannelOptions(1), CancellationToken.None);

		// Act
		QueueStats stats = await queueService.GetQueueStatsAsync(testKey);

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe(testKey);
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Bounded_Should_Return_Null_When_Result_Is_Null()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		BoundedChannelOptions options = new(1);

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<string?>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

		// Act
		string? result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult<string?>(null), options);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task ExecuteQueuedAsync_Unbounded_Should_Return_Null_When_Result_Is_Null()
	{
		// Arrange
		Mock<ControllerBase> controllerMock = new();
		Mock<IEndpointQueueService> serviceMock = new();
		UnboundedChannelOptions options = new();

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<string?>>>(), options, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

		// Act
		string? result = await controllerMock.Object.ExecuteQueuedAsync(serviceMock.Object, _ => Task.FromResult<string?>(null), options);

		// Assert
		result.ShouldBeNull();
	}

	#region Integration Tests

	[Fact]
	public async Task EndpointQueueMetrics_GetAll_Endpoint_Should_Return_Stats_Successfully()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddRouting();
					services.AddSingleton<EndpointQueueService>();
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseEndpoints(endpoints => EndpointQueueExtensions.EndpointQueueMetrics(endpoints));
				}))
			.StartAsync(cancellationToken: Current.CancellationToken);

		HttpClient client = host.GetTestClient();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/endpoint-queue-metrics", Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync(Current.CancellationToken);
		content.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task EndpointQueueMetrics_GetByKey_Endpoint_Should_Return_Stats_For_Specific_Key()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddRouting();
					services.AddSingleton<EndpointQueueService>();
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseEndpoints(endpoints => EndpointQueueExtensions.EndpointQueueMetrics(endpoints));
				}))
			.StartAsync(cancellationToken: Current.CancellationToken);

		HttpClient client = host.GetTestClient();
		EndpointQueueService queueService = host.Services.GetRequiredService<EndpointQueueService>();

		// Execute a task to create the queue
		const string testKey = "TestEndpoint";
		await queueService.ExecuteAsync(testKey, _ => Task.FromResult(42), new BoundedChannelOptions(1), CancellationToken.None);

		// Act
		HttpResponseMessage response = await client.GetAsync($"/api/endpoint-queue-metrics/{testKey}", Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync(Current.CancellationToken);
		content.ShouldContain($"\"EndpointKey\":\"{testKey}\"");
	}

	[Fact]
	public async Task EndpointQueueMetrics_GetAll_When_Service_Throws_Exception_Should_Return_Problem()
	{
		// Arrange - Create a mock service that throws an exception
		Mock<EndpointQueueService> serviceMock = new();
		serviceMock.Setup(s => s.GetAllQueueStatsAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));

		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddRouting();
					services.AddSingleton(serviceMock.Object);
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseEndpoints(endpoints => EndpointQueueExtensions.EndpointQueueMetrics(endpoints));
				}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/endpoint-queue-metrics");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Error retrieving endpoint queue metrics");
		content.ShouldContain("Test exception");
	}

	[Fact]
	public async Task EndpointQueueMetrics_GetByKey_When_Service_Throws_Exception_Should_Return_Problem()
	{
		// Arrange - Create a mock service that throws an exception
		Mock<EndpointQueueService> serviceMock = new();
		serviceMock.Setup(s => s.GetQueueStatsAsync(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("Test exception"));

		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddRouting();
					services.AddSingleton(serviceMock.Object);
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseEndpoints(endpoints => EndpointQueueExtensions.EndpointQueueMetrics(endpoints));
				}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/endpoint-queue-metrics/TestKey");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Error retrieving endpoint queue metrics");
		content.ShouldContain("Test exception");
	}

	#endregion

	// Test controller for endpoint key generation tests
	public class TestController : ControllerBase;
}
