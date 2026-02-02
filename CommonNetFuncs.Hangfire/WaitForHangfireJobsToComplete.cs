using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace CommonNetFuncs.Hangfire;

public static class WaitForHangfireJobsToComplete
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Waits for all Hangfire jobs across all queues to complete
	/// </summary>
	/// <param name="checkIntervalSeconds">How often to check job status (default: 5 seconds)</param>
	/// <param name="maxWaitMinutes">Maximum time to wait for jobs (default: 60 minutes)</param>
	public static async Task WaitForAllHangfireJobsToComplete(int checkIntervalSeconds = 5, int maxWaitMinutes = 60)
	{
		DateTime startTime = DateTime.UtcNow;
		TimeSpan maxWaitTime = TimeSpan.FromMinutes(maxWaitMinutes);

		IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();

		while (true)
		{
			// Check if we've exceeded the maximum wait time
			if (DateTime.UtcNow - startTime > maxWaitTime)
			{
				logger.Warn("Maximum wait time of {maxWait} minutes exceeded. Some jobs may still be pending.", maxWaitMinutes);
				break;
			}

			// Get counts from all possible job states
			StatisticsDto statistics = monitoringApi.GetStatistics();

			long enqueuedCount = statistics.Enqueued; // Jobs waiting to be processed
			long processingCount = statistics.Processing; // Jobs currently running
			long scheduledCount = statistics.Scheduled; // Jobs scheduled for future execution

			long totalPendingJobs = enqueuedCount + processingCount + scheduledCount;

			if (totalPendingJobs == 0)
			{
				logger.Info("No pending Hangfire jobs found");
				break;
			}

			logger.Info(
				"Waiting for {total} Hangfire job(s): {enqueued} enqueued, {processing} processing, {scheduled} scheduled",
				totalPendingJobs, enqueuedCount, processingCount, scheduledCount);

			await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds));
		}
	}
}
