using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommonNetFuncs.Hangfire;

/// <summary>
/// Monitors Hangfire jobs during application shutdown to ensure graceful termination
/// </summary>
public sealed class HangfireShutdownMonitor(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime) : IHostedService
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly IHostApplicationLifetime lifetime = lifetime;
	private readonly IServiceProvider serviceProvider = serviceProvider;

	public Task StartAsync(CancellationToken cancellationToken)
	{
		// Register shutdown monitoring
		lifetime.ApplicationStopping.Register(OnShutdown);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	private void OnShutdown()
	{
		try
		{
			BackgroundJobServer backgroundJobServer = serviceProvider.GetRequiredService<BackgroundJobServer>();
			IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();
			StatisticsDto statistics = monitoringApi.GetStatistics();
			backgroundJobServer.Dispose();
			long processingCount = statistics.Processing;
			long enqueuedCount = statistics.Enqueued;
			long scheduledCount = statistics.Scheduled;
			long totalPending = processingCount + enqueuedCount + scheduledCount;

			if (totalPending > 0)
			{
				logger.Warn(
					"Application shutting down with {total} pending Hangfire job(s): {processing} processing, {enqueued} enqueued, {scheduled} scheduled. " +
					"Jobs will be persisted in database and resumed by next instance.",
					totalPending, processingCount, enqueuedCount, scheduledCount);
			}
			else
			{
				logger.Info("Application shutting down with no pending Hangfire jobs");
			}
		}
		catch (Exception ex)
		{
			logger.Warn(ex, "Error checking Hangfire job status during shutdown");
		}
	}
}
