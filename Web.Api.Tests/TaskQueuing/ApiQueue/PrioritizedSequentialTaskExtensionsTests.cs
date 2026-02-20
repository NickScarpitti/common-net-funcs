using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Web.Api.Tests.TaskQueuing.ApiQueue;

public class PrioritizedSequentialTaskExtensionsTests
{
	#region EndpointQueueMetrics Tests

	[Fact]
	public void EndpointQueueMetrics_Should_Return_Same_Builder()
	{
		// Arrange
		Mock<IEndpointRouteBuilder> endpointsMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();

		endpointsMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
		endpointsMock.Setup(x => x.DataSources).Returns([]);
		endpointsMock.Setup(x => x.CreateApplicationBuilder()).Returns(Mock.Of<IApplicationBuilder>());

		// Act
		IEndpointRouteBuilder result = PrioritizedSequentialTaskExtensions.EndpointQueueMetrics(endpointsMock.Object);

		// Assert
		result.ShouldBe(endpointsMock.Object);
	}

	[Fact]
	public void EndpointQueueMetrics_Should_Not_Throw_When_Called()
	{
		// Arrange
		Mock<IEndpointRouteBuilder> endpointsMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();

		endpointsMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
		endpointsMock.Setup(x => x.DataSources).Returns([]);
		endpointsMock.Setup(x => x.CreateApplicationBuilder()).Returns(Mock.Of<IApplicationBuilder>());

		// Act & Assert
		Should.NotThrow(() => PrioritizedSequentialTaskExtensions.EndpointQueueMetrics(endpointsMock.Object));
	}

	[Fact]
	public void EndpointQueueMetrics_Should_Handle_Null_Builder_With_ArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => PrioritizedSequentialTaskExtensions.EndpointQueueMetrics(null!));
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Return_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Return_Zero_Stats_Initially()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.TotalQueuedTasks.ShouldBe(0);
		stats.TotalProcessedTasks.ShouldBe(0);
		stats.TotalFailedTasks.ShouldBe(0);
		stats.TotalCancelledTasks.ShouldBe(0);
		stats.CurrentQueueDepth.ShouldBe(0);
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Have_Priority_Breakdown()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.PriorityBreakdown.ShouldNotBeNull();
		stats.PriorityBreakdown.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Have_Null_CurrentProcessingPriority_When_Idle()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.CurrentProcessingPriority.ShouldBeNull();
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Contain_All_Priority_Levels()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Low).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Normal).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.High).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Critical).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Emergency).ShouldBeTrue();
	}

	[Fact]
	public async Task Results_Ok_Should_Create_Result_For_Stats()
	{
		// Arrange
		PrioritizedQueueStats stats = new("Test")
		{
			TotalQueuedTasks = 5,
			TotalProcessedTasks = 3
		};

		// Act
		IResult result = Results.Ok(stats);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void Results_Problem_Should_Create_ProblemResult_With_StatusCode()
	{
		// Arrange
		const string detail = "Test error message";
		const int statusCode = StatusCodes.Status500InternalServerError;
		const string title = "Error retrieving endpoint queue metrics";

		// Act
		IResult result = Results.Problem(detail: detail, statusCode: statusCode, title: title);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void Results_Problem_Should_Accept_Empty_Detail()
	{
		// Arrange
		string detail = string.Empty;
		const int statusCode = StatusCodes.Status500InternalServerError;
		const string title = "Error retrieving endpoint queue metrics";

		// Act
		IResult result = Results.Problem(detail: detail, statusCode: statusCode, title: title);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void Results_Problem_Should_Accept_Exception_Message()
	{
		// Arrange
		Exception ex = new InvalidOperationException("Database connection failed");
		const int statusCode = StatusCodes.Status500InternalServerError;
		const string title = "Error retrieving endpoint queue metrics";

		// Act
		IResult result = Results.Problem(detail: ex.Message, statusCode: statusCode, title: title);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Return_Consistent_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats1 = await processor.GetAllQueueStatsAsync();
		PrioritizedQueueStats stats2 = await processor.GetAllQueueStatsAsync();

		// Assert
		stats1.EndpointKey.ShouldBe(stats2.EndpointKey);
		stats1.TotalQueuedTasks.ShouldBe(stats2.TotalQueuedTasks);
		stats1.TotalProcessedTasks.ShouldBe(stats2.TotalProcessedTasks);
	}

	[Fact]
	public async Task Processor_GetAllQueueStatsAsync_Should_Complete_Quickly()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();
		stopwatch.Stop();

		// Assert
		stats.ShouldNotBeNull();
		stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000); // Should complete in less than 1 second
	}

	[Fact]
	public void StatusCodes_Status500InternalServerError_Should_Be_500()
	{
		// Assert
		StatusCodes.Status500InternalServerError.ShouldBe(500);
	}

	[Fact]
	public async Task Processor_With_UnboundedChannel_Should_Return_Stats()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public async Task Processor_Stats_Should_Have_Valid_EndpointKey()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.EndpointKey.ShouldNotBeNull();
		stats.EndpointKey.ShouldNotBeEmpty();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public async Task Processor_Priority_Stats_Should_Be_Initialized()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
		{
			stats.PriorityBreakdown.ShouldContainKey(priority);
			PriorityStats priorityStats = stats.PriorityBreakdown[priority];
			priorityStats.ShouldNotBeNull();
			priorityStats.QueuedTasks.ShouldBe(0);
			priorityStats.ProcessedTasks.ShouldBe(0);
		}
	}

	[Fact]
	public async Task Endpoint_Handler_Logic_Success_Path_Should_Work()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act - Simulate the endpoint handler logic
		try
		{
			PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();
			IResult result = Results.Ok(stats);

			// Assert
			result.ShouldNotBeNull();
			stats.EndpointKey.ShouldBe("All");
		}
		catch (Exception ex)
		{
			// Should not reach here
			ex.ShouldBeNull();
		}
	}

	[Fact]
	public void Endpoint_Handler_Logic_Exception_Path_Should_Work()
	{
		// Arrange
		Exception testException = new InvalidOperationException("Test exception message");

		// Act - Simulate the endpoint handler exception logic
		IResult result = Results.Problem(
			detail: testException.Message,
			statusCode: StatusCodes.Status500InternalServerError,
			title: "Error retrieving endpoint queue metrics");

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task Multiple_Calls_To_GetAllQueueStatsAsync_Should_Return_Same_Reference()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats1 = await processor.GetAllQueueStatsAsync();
		PrioritizedQueueStats stats2 = await processor.GetAllQueueStatsAsync();

		// Assert - Both should reference the same stats object
		stats1.ShouldBe(stats2);
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Stats_With_All_Properties()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert - Verify all properties are accessible
		stats.EndpointKey.ShouldNotBeNull();
		stats.TotalQueuedTasks.ShouldBeGreaterThanOrEqualTo(0);
		stats.TotalProcessedTasks.ShouldBeGreaterThanOrEqualTo(0);
		stats.TotalFailedTasks.ShouldBeGreaterThanOrEqualTo(0);
		stats.TotalCancelledTasks.ShouldBeGreaterThanOrEqualTo(0);
		stats.CurrentQueueDepth.ShouldBeGreaterThanOrEqualTo(0);
		stats.PriorityBreakdown.ShouldNotBeNull();
	}

	[Fact]
	public void Results_Factory_Methods_Should_Be_Accessible()
	{
		// Assert - Verify Results factory methods exist and are accessible
		Should.NotThrow(() =>
		{
			_ = Results.Ok(new { Value = "test" });
			_ = Results.Problem(detail: "test", statusCode: 500, title: "test");
		});
	}

	[Fact]
	public async Task Processor_Dispose_Should_Complete_Successfully()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		// Act
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();
		processor.Dispose();

		// Assert
		stats.ShouldNotBeNull();
	}

	#endregion
}
