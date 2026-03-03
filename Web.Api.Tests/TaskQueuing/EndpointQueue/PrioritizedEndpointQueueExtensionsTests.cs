using System.Net;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using static Xunit.TestContext;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public sealed class PrioritizedEndpointQueueExtensionsTests
{
	[Theory]
	[InlineData(TaskPriority.Low, null)]
	[InlineData(TaskPriority.Normal, "customKey")]
	public async Task ExecutePrioritizedAsync_Priority_Should_Invoke_Service(TaskPriority priority, string? customKey)
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), priority, It.IsAny<CancellationToken>())).ReturnsAsync(321);

		int result = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(321), priority, customKey);

		result.ShouldBe(321);
	}

	[Theory]
	[InlineData(1, null)]
	[InlineData(2, "customKey")]
	public async Task ExecutePrioritizedAsync_CustomPriority_Should_Invoke_Service(int customPriority, string? customKey)
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), customPriority, It.IsAny<CancellationToken>())).ReturnsAsync(654);

		int result = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(654), customPriority, customKey);

		result.ShouldBe(654);
	}

	[Fact]
	public async Task ExecutePrioritizedAsync_With_CustomKey_Should_Use_CustomKey()
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		string capturedKey = string.Empty;
		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.Callback<string, Func<CancellationToken, Task<int>>, int, CancellationToken>((key, _, __, ___) => capturedKey = key).ReturnsAsync(999);

		_ = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(999), 1, "my-custom-key");

		capturedKey.ShouldBe("my-custom-key");
	}

	[Fact]
	public async Task ExecutePrioritizedAsync_Should_Return_Task_Result()
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("result-value");

		string? result = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult("result-value"), 1);

		result.ShouldBe("result-value");
	}

	[Fact]
	public async Task ExecutePrioritizedAsync_Should_Pass_Priority_To_Service()
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		int capturedPriority = 0;
		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.Callback<string, Func<CancellationToken, Task<int>>, int, CancellationToken>((_, __, priority, ___) => capturedPriority = priority).ReturnsAsync(777);

		_ = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(777), 5);

		capturedPriority.ShouldBe(5);
	}

	[Fact]
	public async Task ExecutePrioritizedAsync_With_TaskPriority_Enum_Should_Convert_To_Int()
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		int capturedPriority = 0;
		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<TaskPriority>(), It.IsAny<CancellationToken>()))
			.Callback<string, Func<CancellationToken, Task<int>>, TaskPriority, CancellationToken>((_, __, priority, ___) => capturedPriority = (int)priority).ReturnsAsync(888);

		_ = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, _ => Task.FromResult(888), TaskPriority.High);

		capturedPriority.ShouldBe((int)TaskPriority.High);
	}

	[Fact]
	public async Task ExecutePrioritizedAsync_Should_Invoke_TaskFunction()
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		bool taskFunctionInvoked = false;
		Task<bool> TaskFunction(CancellationToken _)
		{
			taskFunctionInvoked = true;
			return Task.FromResult(true);
		}

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.Returns(async (string _, Func<CancellationToken, Task<bool>> func, int __, CancellationToken ct) => await func(ct));

		_ = await controllerMock.Object.ExecutePrioritizedAsync(serviceMock.Object, TaskFunction, 1);

		taskFunctionInvoked.ShouldBeTrue();
	}

	[Fact]
	public async Task ExecutePrioritizedAsync_Should_Return_Null_When_Service_Returns_Null()
	{
		Mock<ControllerBase> controllerMock = new();
		Mock<IPrioritizedEndpointQueueService> serviceMock = new();

		serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<string?>>>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
		string? result = await controllerMock.Object.ExecutePrioritizedAsync<string>(serviceMock.Object, _ => Task.FromResult<string?>(null), 1);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		result.ShouldBeNull();
	}

	[Fact]
	public void EndpointQueueMetrics_Should_Return_Same_EndpointRouteBuilder()
	{
		// Arrange
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		WebApplication app = builder.Build();

		// Act
		IEndpointRouteBuilder result = PrioritizedEndpointQueueExtensions.EndpointQueueMetrics(app);

		// Assert
		result.ShouldBe(app);
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
					services.AddSingleton<PrioritizedEndpointQueueService>();
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseEndpoints(endpoints => PrioritizedEndpointQueueExtensions.EndpointQueueMetrics(endpoints));
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
					services.AddSingleton<PrioritizedEndpointQueueService>();
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseEndpoints(endpoints => PrioritizedEndpointQueueExtensions.EndpointQueueMetrics(endpoints));
				}))
			.StartAsync(cancellationToken: Current.CancellationToken);

		HttpClient client = host.GetTestClient();
		PrioritizedEndpointQueueService queueService = host.Services.GetRequiredService<PrioritizedEndpointQueueService>();

		// Execute a task to create the queue
		const string testKey = "TestPriorityEndpoint";
		await queueService.ExecuteAsync(testKey, _ => Task.FromResult(42), TaskPriority.Normal, CancellationToken.None);

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
		Mock<PrioritizedEndpointQueueService> serviceMock = new();
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
					app.UseEndpoints(endpoints => PrioritizedEndpointQueueExtensions.EndpointQueueMetrics(endpoints));
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
		Mock<PrioritizedEndpointQueueService> serviceMock = new();
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
					app.UseEndpoints(endpoints => PrioritizedEndpointQueueExtensions.EndpointQueueMetrics(endpoints));
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

	public class TestController : ControllerBase;
}
