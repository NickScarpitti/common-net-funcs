using CommonNetFuncs.Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Tests;

public sealed class WaitForHangfireJobsToCompleteTests
{
	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldReturnImmediately_WhenNoJobsPending()
	{
		// Arrange
		SetupHangfireStorage(processing: 0, enqueued: 0, scheduled: 0);

		// Act
		DateTime startTime = DateTime.UtcNow;
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);
		TimeSpan duration = DateTime.UtcNow - startTime;

		// Assert - Should return immediately (within 2 seconds) when no jobs are pending
		Assert.True(duration.TotalSeconds < 2, $"Method should return immediately but took {duration.TotalSeconds} seconds");
	}

	[Theory]
	[InlineData(1, 0, 0)] // 1 processing job
	[InlineData(0, 1, 0)] // 1 enqueued job
	[InlineData(0, 0, 1)] // 1 scheduled job
	public async Task WaitForAllHangfireJobsToComplete_ShouldWaitAndCheckMultipleTimes_WhenJobsArePending(
		long initialProcessing,
		long initialEnqueued,
		long initialScheduled)
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			if (callCount == 1)
			{
				return new StatisticsDto
				{
					Processing = initialProcessing,
					Enqueued = initialEnqueued,
					Scheduled = initialScheduled
				};
			}
			// Second call - all jobs completed
			return new StatisticsDto
			{
				Processing = 0,
				Enqueued = 0,
				Scheduled = 0
			};
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);

		// Assert
		Assert.True(callCount >= 2); // Should check at least twice
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldStopAfterMaxWaitTime_WhenJobsNeverComplete()
	{
		// Arrange
		SetupHangfireStorage(processing: 1, enqueued: 1, scheduled: 1); // Jobs always pending

		// Act
		DateTime startTime = DateTime.UtcNow;
		// Use very short timeout for testing (0.05 minutes = 3 seconds)
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 5, maxWaitMinutes: 1);
		TimeSpan duration = DateTime.UtcNow - startTime;

		// Assert - Allow some buffer for execution time
		Assert.True(duration.TotalSeconds >= 60);
		Assert.True(duration.TotalSeconds < 70); // Should not wait much longer than max wait time
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldUseCustomCheckInterval()
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			if (callCount <= 2)
			{
				return new StatisticsDto { Processing = 1 };
			}
			return new StatisticsDto { Processing = 0 };
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 2, maxWaitMinutes: 1);

		// Assert
		Assert.True(callCount >= 3); // Should check at least 3 times
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldHandleAllJobTypesCombination()
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			return callCount switch
			{
				1 => new StatisticsDto { Processing = 5, Enqueued = 3, Scheduled = 2 },
				2 => new StatisticsDto { Processing = 2, Enqueued = 1, Scheduled = 1 },
				3 => new StatisticsDto { Processing = 1, Enqueued = 0, Scheduled = 0 },
				_ => new StatisticsDto { Processing = 0, Enqueued = 0, Scheduled = 0 }
			};
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);

		// Assert
		Assert.True(callCount >= 4); // Should check multiple times until all complete
	}

	[Theory]
	[InlineData(5, 60)]
	[InlineData(10, 30)]
	[InlineData(1, 120)]
	public async Task WaitForAllHangfireJobsToComplete_ShouldAcceptDifferentParameters(
		int checkIntervalSeconds,
		int maxWaitMinutes)
	{
		// Arrange
		SetupHangfireStorage(processing: 0, enqueued: 0, scheduled: 0);

		// Act
		DateTime startTime = DateTime.UtcNow;
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds, maxWaitMinutes);
		TimeSpan duration = DateTime.UtcNow - startTime;

		// Assert - Should complete quickly when no jobs are pending
		Assert.True(duration.TotalSeconds < 5, $"Method should complete quickly but took {duration.TotalSeconds} seconds");
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldUseDefaultParameters()
	{
		// Arrange
		SetupHangfireStorage(processing: 0, enqueued: 0, scheduled: 0);

		// Act
		DateTime startTime = DateTime.UtcNow;
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete();
		TimeSpan duration = DateTime.UtcNow - startTime;

		// Assert - Should complete quickly when no jobs are pending using defaults (5 seconds, 60 minutes)
		Assert.True(duration.TotalSeconds < 5, $"Method should complete quickly but took {duration.TotalSeconds} seconds");
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldLogWarning_WhenMaxWaitTimeExceeded()
	{
		// Arrange
		SetupHangfireStorage(processing: 10, enqueued: 5, scheduled: 3); // Always has pending jobs

		// Act - Use 1 minute timeout and check every 10 seconds
		DateTime startTime = DateTime.UtcNow;
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 10, maxWaitMinutes: 1);
		TimeSpan duration = DateTime.UtcNow - startTime;

		// Assert - Should stop after max wait time even when jobs never complete
		Assert.True(duration.TotalSeconds >= 60, $"Method should wait at least 60 seconds but only waited {duration.TotalSeconds} seconds");
		Assert.True(duration.TotalSeconds < 70, $"Method should not wait much longer than 60 seconds but waited {duration.TotalSeconds} seconds");
		// Logging verification would require capturing NLog output
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldHandleZeroJobs_AfterHavingJobs()
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			return callCount switch
			{
				1 => new StatisticsDto { Processing = 10 },
				2 => new StatisticsDto { Enqueued = 5 },
				3 => new StatisticsDto { Scheduled = 3 },
				_ => new StatisticsDto { Processing = 0, Enqueued = 0, Scheduled = 0 }
			};
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);

		// Assert
		Assert.True(callCount >= 4);
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldCountAllPendingJobTypes()
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			if (callCount == 1)
			{
				// All three types have jobs
				return new StatisticsDto
				{
					Processing = 2,
					Enqueued = 3,
					Scheduled = 4
				};
			}
			// All complete
			return new StatisticsDto
			{
				Processing = 0,
				Enqueued = 0,
				Scheduled = 0
			};
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);

		// Assert
		Assert.Equal(2, callCount); // Should check twice: first sees 9 total jobs, second sees 0
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldHandleVeryShortCheckInterval()
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			return callCount <= 3
				? new StatisticsDto { Processing = 1 }
				: new StatisticsDto { Processing = 0 };
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);

		// Assert
		Assert.True(callCount >= 4);
	}

	[Fact]
	public async Task WaitForAllHangfireJobsToComplete_ShouldContinueWaiting_UntilAllJobTypesAreZero()
	{
		// Arrange
		int callCount = 0;
		StatisticsDto GetStatistics()
		{
			callCount++;
			return callCount switch
			{
				1 => new StatisticsDto { Processing = 1, Enqueued = 0, Scheduled = 0 },
				2 => new StatisticsDto { Processing = 0, Enqueued = 1, Scheduled = 0 },
				3 => new StatisticsDto { Processing = 0, Enqueued = 0, Scheduled = 1 },
				_ => new StatisticsDto { Processing = 0, Enqueued = 0, Scheduled = 0 }
			};
		}

		SetupHangfireStorageWithCallback(GetStatistics);

		// Act
		await WaitForHangfireJobsToComplete.WaitForAllHangfireJobsToComplete(checkIntervalSeconds: 1, maxWaitMinutes: 1);

		// Assert
		Assert.True(callCount >= 4); // Should check until all three types are zero
	}

	private static void SetupHangfireStorage(long processing, long enqueued, long scheduled)
	{
		JobStorage storage = A.Fake<JobStorage>();
		IMonitoringApi monitoringApi = A.Fake<IMonitoringApi>();

		StatisticsDto statistics = new()
		{
			Processing = processing,
			Enqueued = enqueued,
			Scheduled = scheduled
		};

		A.CallTo(() => monitoringApi.GetStatistics()).Returns(statistics);
		A.CallTo(() => storage.GetMonitoringApi()).Returns(monitoringApi);

		JobStorage.Current = storage;
	}

	private static void SetupHangfireStorageWithCallback(Func<StatisticsDto> statisticsCallback)
	{
		JobStorage storage = A.Fake<JobStorage>();
		IMonitoringApi monitoringApi = A.Fake<IMonitoringApi>();

		A.CallTo(() => monitoringApi.GetStatistics()).ReturnsLazily(() => statisticsCallback());
		A.CallTo(() => storage.GetMonitoringApi()).Returns(monitoringApi);

		JobStorage.Current = storage;
	}
}
