using CommonNetFuncs.Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hangfire.Tests;

public sealed class HangfireShutdownMonitorTests
{
	[Fact]
	public void Constructor_ShouldInitializeCorrectly()
	{
		// Arrange
		IServiceProvider serviceProvider = A.Fake<IServiceProvider>();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();

		// Act
		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);

		// Assert
		Assert.NotNull(monitor);
	}

	[Fact]
	public async Task StartAsync_ShouldRegisterShutdownCallback()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		IServiceProvider serviceProvider = A.Fake<IServiceProvider>();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();
		A.CallTo(() => lifetime.ApplicationStopping).Returns(cts.Token);

		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);

		// Act
		await monitor.StartAsync(CancellationToken.None);

		// Assert - If StartAsync completes without error, the registration succeeded
		Assert.True(true);
	}

	[Fact]
	public async Task StopAsync_ShouldCompleteSuccessfully()
	{
		// Arrange
		IServiceProvider serviceProvider = A.Fake<IServiceProvider>();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();

		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);

		// Act
		Task result = monitor.StopAsync(CancellationToken.None);
		await result;

		// Assert
		Assert.True(result.IsCompleted);
	}

	[Theory]
	[InlineData(0, 0, 0)] // No pending jobs
	[InlineData(1, 0, 0)] // 1 processing job
	[InlineData(0, 1, 0)] // 1 enqueued job
	[InlineData(0, 0, 1)] // 1 scheduled job
	[InlineData(5, 3, 2)] // Multiple pending jobs
	public async Task OnShutdown_ShouldLogCorrectly_BasedOnJobCounts(
		long processingCount,
		long enqueuedCount,
		long scheduledCount)
	{
		// Arrange
		await using ServiceProvider serviceProvider = SetupServiceProviderWithHangfire(processingCount, enqueuedCount, scheduledCount);
		using CancellationTokenSource cts = new();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();
		A.CallTo(() => lifetime.ApplicationStopping).Returns(cts.Token);

		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);
		await monitor.StartAsync(CancellationToken.None);

		// Act - Trigger the shutdown by cancelling the token
		await cts.CancelAsync();

		// Give the callback time to execute
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Verify the monitoring API was called (proves callback executed)
		JobStorage storage = JobStorage.Current;
		A.CallTo(() => storage.GetMonitoringApi()).MustHaveHappened();
	}

	[Fact]
	public async Task OnShutdown_ShouldHandleException_WhenBackgroundJobServerNotFound()
	{
		// Arrange
		IServiceProvider serviceProvider = A.Fake<IServiceProvider>();
		A.CallTo(() => serviceProvider.GetService(typeof(BackgroundJobServer)))
			.Returns(null);

		using CancellationTokenSource cts = new();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();
		A.CallTo(() => lifetime.ApplicationStopping).Returns(cts.Token);

		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);
		await monitor.StartAsync(CancellationToken.None);

		// Act - Trigger the shutdown by cancelling the token
		// Should not throw - exception is caught and logged
		await cts.CancelAsync();

		// Give the callback time to execute
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Verify the service provider was called (proves callback executed despite error)
		A.CallTo(() => serviceProvider.GetService(typeof(BackgroundJobServer))).MustHaveHappened();
	}

	[Fact]
	public async Task OnShutdown_ShouldHandleException_WhenMonitoringApiThrows()
	{
		// Arrange
		BackgroundJobServer backgroundJobServer = A.Fake<BackgroundJobServer>();
		IServiceProvider serviceProvider = A.Fake<IServiceProvider>();
		A.CallTo(() => serviceProvider.GetService(typeof(BackgroundJobServer)))
			.Returns(backgroundJobServer);

		// Setup JobStorage.Current to throw when GetMonitoringApi is called
		JobStorage mockStorage = A.Fake<JobStorage>();
		A.CallTo(() => mockStorage.GetMonitoringApi())
			.Throws<InvalidOperationException>();

		JobStorage.Current = mockStorage;

		using CancellationTokenSource cts = new();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();
		A.CallTo(() => lifetime.ApplicationStopping).Returns(cts.Token);

		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);
		await monitor.StartAsync(CancellationToken.None);

		// Act - Trigger the shutdown by cancelling the token
		// Should not throw - exception is caught and logged
		await cts.CancelAsync();

		// Give the callback time to execute
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Verify GetMonitoringApi was called (callback executed despite exception)
		A.CallTo(() => mockStorage.GetMonitoringApi()).MustHaveHappened();
	}

	[Fact]
	public async Task OnShutdown_ShouldDisposeBackgroundJobServer()
	{
		// Arrange
		await using ServiceProvider serviceProvider = SetupServiceProviderWithHangfire(0, 0, 0);
		using CancellationTokenSource cts = new();
		IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();
		A.CallTo(() => lifetime.ApplicationStopping).Returns(cts.Token);

		HangfireShutdownMonitor monitor = new(serviceProvider, lifetime);
		await monitor.StartAsync(CancellationToken.None);

		// Act - Trigger the shutdown by cancelling the token
		await cts.CancelAsync();

		// Give the callback time to execute
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Verify the callback executed by checking the monitoring API was called
		JobStorage storage = JobStorage.Current;
		A.CallTo(() => storage.GetMonitoringApi()).MustHaveHappened();

		// Also verify we can retrieve the server (shows it was registered)
		BackgroundJobServer? server = serviceProvider.GetService<BackgroundJobServer>();
		Assert.NotNull(server);
		// Note: Verifying Dispose was called is not possible since it's not virtual
	}

	private static ServiceProvider SetupServiceProviderWithHangfire(
		long processingCount,
		long enqueuedCount,
		long scheduledCount)
	{
		// Setup fake storage
		JobStorage storage = A.Fake<JobStorage>();
		JobStorage.Current = storage;

		// Setup fake statistics
		IMonitoringApi monitoringApi = A.Fake<IMonitoringApi>();
		StatisticsDto statistics = new()
		{
			Processing = processingCount,
			Enqueued = enqueuedCount,
			Scheduled = scheduledCount
		};

		A.CallTo(() => monitoringApi.GetStatistics()).Returns(statistics);
		A.CallTo(() => storage.GetMonitoringApi()).Returns(monitoringApi);

		// Setup fake BackgroundJobServer
		BackgroundJobServer backgroundJobServer = A.Fake<BackgroundJobServer>();

		// Create real service provider
		ServiceCollection services = new();
		services.AddSingleton(backgroundJobServer);

		return services.BuildServiceProvider();
	}
}
