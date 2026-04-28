using System.Linq.Expressions;
using System.Reflection;
using AutoFixture;
using CommonNetFuncs.Hangfire;
using FakeItEasy;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Tests;

// Helper interface used to create Hangfire Job objects for testing
public interface ITestJobService
{
	void RunJob(int id, string name);
	void ProcessItem(Guid itemId, bool force);
	void DoWork();
	void SingleArg(int value);
}

public sealed class HangfireJobHelpersTests
{
	private readonly Fixture fixture = new();

	/// <summary>
	/// Creates a Hangfire <see cref="Job"/> object backed by a method on <see cref="ITestJobService"/>.
	/// </summary>
	private static Job CreateJob(string methodName, params object[] args)
	{
		MethodInfo method = typeof(ITestJobService).GetMethod(methodName)!;
		return new Job(typeof(ITestJobService), method, args);
	}

	/// <summary>
	/// Sets up a fake <see cref="JobStorage.Current"/> with optional job lists and a connection whose
	/// "Queue" parameter can be pre-configured.
	/// </summary>
	private static (JobStorage Storage, IMonitoringApi MonitoringApi, IStorageConnection Connection) SetupFakeStorage(
		JobList<EnqueuedJobDto>? enqueuedJobs = null,
		JobList<ProcessingJobDto>? processingJobs = null,
		JobList<ScheduledJobDto>? scheduledJobs = null,
		string? connectionJobQueue = null)
	{
		JobStorage storage = A.Fake<JobStorage>();
		IMonitoringApi monitoringApi = A.Fake<IMonitoringApi>();
		IStorageConnection connection = A.Fake<IStorageConnection>();

		A.CallTo(() => storage.GetMonitoringApi()).Returns(monitoringApi);
		A.CallTo(() => storage.GetConnection()).Returns(connection);

		A.CallTo(() => monitoringApi.EnqueuedJobs(A<string>._, A<int>._, A<int>._))
			.Returns(enqueuedJobs ?? new JobList<EnqueuedJobDto>([]));
		A.CallTo(() => monitoringApi.ProcessingJobs(A<int>._, A<int>._))
			.Returns(processingJobs ?? new JobList<ProcessingJobDto>([]));
		A.CallTo(() => monitoringApi.ScheduledJobs(A<int>._, A<int>._))
			.Returns(scheduledJobs ?? new JobList<ScheduledJobDto>([]));

		A.CallTo(() => connection.GetJobParameter(A<string>._, "Queue"))
			.Returns(connectionJobQueue);

		JobStorage.Current = storage;
		return (storage, monitoringApi, connection);
	}

	#region IsJobMatch — null / method-name matching

	[Fact]
	public void IsJobMatch_WhenJobIsNull_ReturnsFalse()
	{
		// Act
		bool result = HangfireJobHelpers.IsJobMatch(null, "RunJob", new Dictionary<string, string>());

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsJobMatch_WhenMethodNameDoesNotMatch_ReturnsFalse()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 1, "test");

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, "NonExistentMethod", new Dictionary<string, string>());

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsJobMatch_WhenMethodNameMatchesExactly_ReturnsTrue()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.DoWork));

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.DoWork), new Dictionary<string, string>());

		// Assert
		Assert.True(result);
	}

	[Theory]
	[InlineData("dowork")]
	[InlineData("DOWORK")]
	[InlineData("DoWork")]
	public void IsJobMatch_WhenMethodNameMatchesCaseInsensitively_ReturnsTrue(string methodNameVariant)
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.DoWork));

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, methodNameVariant, new Dictionary<string, string>());

		// Assert
		Assert.True(result);
	}

	#endregion

	#region IsJobMatch — empty matchParameters

	[Fact]
	public void IsJobMatch_WhenMatchParametersIsEmpty_ReturnsTrue()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), new Dictionary<string, string>());

		// Assert
		Assert.True(result);
	}

	#endregion

	#region IsJobMatch — parameter value matching

	[Fact]
	public void IsJobMatch_WhenAllParametersMatch_ReturnsTrue()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "id", "42" },
			{ "name", "hello" }
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobMatch_WhenSubsetOfParametersMatch_ReturnsTrue()
	{
		// Arrange — only specify one of the two parameters; the other is unconstrained
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "id", "42" }
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobMatch_WhenParameterValueDoesNotMatch_ReturnsFalse()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "id", "99" } // wrong value
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsJobMatch_WhenOneOfMultipleParameterValuesDoesNotMatch_ReturnsFalse()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "id", "42" },
			{ "name", "world" } // wrong value
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.False(result);
	}

	#endregion

	#region IsJobMatch — case-insensitive key and value matching

	[Fact]
	public void IsJobMatch_WhenParameterKeyIsUppercase_ReturnsTrue()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "ID", "42" },    // uppercase key
			{ "NAME", "hello" } // uppercase key
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobMatch_WhenParameterValueDiffersInCase_ReturnsTrue()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "Hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "name", "hello" } // lower vs "Hello" stored in job
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.True(result);
	}

	#endregion

	#region IsJobMatch — missing or out-of-range parameter names

	[Fact]
	public void IsJobMatch_WhenParameterNameNotFoundOnMethod_ReturnsFalse()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "hello");
		Dictionary<string, string> matchParameters = new()
		{
			{ "nonExistentParam", "someValue" }
		};

		// Act
		bool result = HangfireJobHelpers.IsJobMatch(job, nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.False(result);
	}

	#endregion

	#region IsJobInQueue — overload with IStorageConnection

	[Fact]
	public void IsJobInQueue_WithConnection_WhenQueueParamIsNull_ReturnsTrue()
	{
		// Arrange — null means "no queue recorded", conservatively allow it
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Returns(null);

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue", connection);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobInQueue_WithConnection_WhenQueueParamMatchesExactly_ReturnsTrue()
	{
		// Arrange
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Returns("my-queue");

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue", connection);

		// Assert
		Assert.True(result);
	}

	[Theory]
	[InlineData("MY-QUEUE")]
	[InlineData("My-Queue")]
	[InlineData("my-queue")]
	public void IsJobInQueue_WithConnection_WhenQueueParamMatchesCaseInsensitively_ReturnsTrue(string storedQueue)
	{
		// Arrange
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Returns(storedQueue);

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue", connection);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobInQueue_WithConnection_WhenQueueParamDoesNotMatch_ReturnsFalse()
	{
		// Arrange
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Returns("other-queue");

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue", connection);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsJobInQueue_WithConnection_WhenGetJobParameterThrows_ReturnsTrue()
	{
		// Arrange — exception during lookup should conservatively allow the job
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Throws<InvalidOperationException>();

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue", connection);

		// Assert
		Assert.True(result);
	}

	#endregion

	#region IsJobInQueue — overload that creates its own connection via JobStorage.Current

	[Fact]
	public void IsJobInQueue_WhenQueueParamMatchesExactly_ReturnsTrue()
	{
		// Arrange
		JobStorage storage = A.Fake<JobStorage>();
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => storage.GetConnection()).Returns(connection);
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Returns("my-queue");
		JobStorage.Current = storage;

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue");

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobInQueue_WhenQueueParamDoesNotMatch_ReturnsFalse()
	{
		// Arrange
		JobStorage storage = A.Fake<JobStorage>();
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => storage.GetConnection()).Returns(connection);
		A.CallTo(() => connection.GetJobParameter("job-1", "Queue")).Returns("different-queue");
		JobStorage.Current = storage;

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue");

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void IsJobInQueue_WhenGetConnectionThrows_ReturnsTrue()
	{
		// Arrange — storage failure should conservatively allow the job
		JobStorage storage = A.Fake<JobStorage>();
		A.CallTo(() => storage.GetConnection()).Throws<InvalidOperationException>();
		JobStorage.Current = storage;

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue");

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsJobInQueue_WhenQueueParamIsNull_ReturnsTrue()
	{
		// Arrange
		JobStorage storage = A.Fake<JobStorage>();
		IStorageConnection connection = A.Fake<IStorageConnection>();
		A.CallTo(() => storage.GetConnection()).Returns(connection);
		A.CallTo(() => connection.GetJobParameter(A<string>._, "Queue")).Returns(null);
		JobStorage.Current = storage;

		// Act
		bool result = HangfireJobHelpers.IsJobInQueue("job-1", "my-queue");

		// Assert
		Assert.True(result);
	}

	#endregion

	#region FindAndCancelJobs<TJob> — expression-based overload

	[Fact]
	public void FindAndCancelJobs_Generic_WhenExpressionBodyIsNotMethodCall_ThrowsArgumentException()
	{
		// Arrange — build a lambda whose body is not a MethodCallExpression
		SetupFakeStorage();
		ParameterExpression param = Expression.Parameter(typeof(ITestJobService), "j");
		Expression<Action<ITestJobService>> nonMethodExpr =
			Expression.Lambda<Action<ITestJobService>>(Expression.Empty(), param);

		// Act & Assert
		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => "my-queue".FindAndCancelJobs(nonMethodExpr));
		Assert.Contains("jobExpression", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void FindAndCancelJobs_Generic_WhenNoJobsExist_ReturnsEmptyList()
	{
		// Arrange
		SetupFakeStorage();
		int id = fixture.Create<int>();
		string name = fixture.Create<string>();

		// Act
		List<string> result = "my-queue".FindAndCancelJobs<ITestJobService>(j => j.RunJob(id, name));

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_Generic_WhenArgumentIsNull_ExcludesItFromMatchParameters()
	{
		// Arrange — null values must be excluded from the match dictionary (not cause an exception)
		SetupFakeStorage();

		// Act
		List<string> result = "my-queue".FindAndCancelJobs<ITestJobService>(j => j.RunJob(1, null!));

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_Generic_WhenEnqueuedJobMatchesExpression_CallsMonitoringApi()
	{
		// Arrange
		var (_, monitoringApi, _) = SetupFakeStorage();
		int id = fixture.Create<int>();
		string name = fixture.Create<string>();

		// Act
		"my-queue".FindAndCancelJobs<ITestJobService>(j => j.RunJob(id, name));

		// Assert — monitoring API must have been consulted for enqueued jobs
		A.CallTo(() => monitoringApi.EnqueuedJobs("my-queue", 0, int.MaxValue))
			.MustHaveHappenedOnceExactly();
	}

	#endregion

	#region FindAndCancelJobs — string-based overload, monitoring API surface

	[Fact]
	public void FindAndCancelJobs_WhenAllJobListsAreEmpty_ReturnsEmptyList()
	{
		// Arrange
		SetupFakeStorage();

		// Act
		List<string> result = "my-queue".FindAndCancelJobs("RunJob", new Dictionary<string, string>());

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_CallsEnqueuedJobsWithCorrectQueueName()
	{
		// Arrange
		var (_, monitoringApi, _) = SetupFakeStorage();
		string queueName = fixture.Create<string>();

		// Act
		queueName.FindAndCancelJobs("RunJob", new Dictionary<string, string>());

		// Assert
		A.CallTo(() => monitoringApi.EnqueuedJobs(queueName, 0, int.MaxValue))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void FindAndCancelJobs_CallsProcessingAndScheduledJobsWithFullRange()
	{
		// Arrange
		var (_, monitoringApi, _) = SetupFakeStorage();

		// Act
		"my-queue".FindAndCancelJobs("RunJob", new Dictionary<string, string>());

		// Assert
		A.CallTo(() => monitoringApi.ProcessingJobs(0, int.MaxValue)).MustHaveHappenedOnceExactly();
		A.CallTo(() => monitoringApi.ScheduledJobs(0, int.MaxValue)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public void FindAndCancelJobs_CallsAllThreeMonitoringApiEndpoints()
	{
		// Arrange
		var (_, monitoringApi, _) = SetupFakeStorage();

		// Act
		"my-queue".FindAndCancelJobs("RunJob", new Dictionary<string, string>());

		// Assert
		A.CallTo(() => monitoringApi.EnqueuedJobs(A<string>._, A<int>._, A<int>._))
			.MustHaveHappenedOnceExactly();
		A.CallTo(() => monitoringApi.ProcessingJobs(A<int>._, A<int>._))
			.MustHaveHappenedOnceExactly();
		A.CallTo(() => monitoringApi.ScheduledJobs(A<int>._, A<int>._))
			.MustHaveHappenedOnceExactly();
	}

	#endregion

	#region FindAndCancelJobs — non-matching jobs

	[Fact]
	public void FindAndCancelJobs_WhenEnqueuedJobMethodNameDoesNotMatch_ReturnsEmptyList()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 1, "test");
		JobList<EnqueuedJobDto> enqueuedJobs = new(
		[
			new KeyValuePair<string, EnqueuedJobDto>("job-1",
				new EnqueuedJobDto { Job = job, State = "Enqueued" })
		]);
		SetupFakeStorage(enqueuedJobs: enqueuedJobs);

		// Act
		List<string> result = "my-queue".FindAndCancelJobs("DifferentMethod", new Dictionary<string, string>());

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenEnqueuedJobParameterValueDoesNotMatch_ReturnsEmptyList()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 1, "test");
		JobList<EnqueuedJobDto> enqueuedJobs = new(
		[
			new KeyValuePair<string, EnqueuedJobDto>("job-1",
				new EnqueuedJobDto { Job = job, State = "Enqueued" })
		]);
		SetupFakeStorage(enqueuedJobs: enqueuedJobs);
		Dictionary<string, string> matchParameters = new() { { "id", "999" } }; // wrong value

		// Act
		List<string> result = "my-queue".FindAndCancelJobs(nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenProcessingJobMethodNameDoesNotMatch_ReturnsEmptyList()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 1, "test");
		JobList<ProcessingJobDto> processingJobs = new(
		[
			new KeyValuePair<string, ProcessingJobDto>("job-2",
				new ProcessingJobDto { Job = job, StartedAt = DateTime.UtcNow })
		]);
		SetupFakeStorage(processingJobs: processingJobs, connectionJobQueue: "my-queue");

		// Act
		List<string> result = "my-queue".FindAndCancelJobs("DifferentMethod", new Dictionary<string, string>());

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenScheduledJobMethodNameDoesNotMatch_ReturnsEmptyList()
	{
		// Arrange
		Job job = CreateJob(nameof(ITestJobService.RunJob), 1, "test");
		JobList<ScheduledJobDto> scheduledJobs = new(
		[
			new KeyValuePair<string, ScheduledJobDto>("job-3",
				new ScheduledJobDto { Job = job, EnqueueAt = DateTime.UtcNow.AddMinutes(5) })
		]);
		SetupFakeStorage(scheduledJobs: scheduledJobs, connectionJobQueue: "my-queue");

		// Act
		List<string> result = "my-queue".FindAndCancelJobs("DifferentMethod", new Dictionary<string, string>());

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenProcessingJobBelongsToDifferentQueue_ReturnsEmptyList()
	{
		// Arrange — job matches method + params but lives in a different queue
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "test");
		JobList<ProcessingJobDto> processingJobs = new(
		[
			new KeyValuePair<string, ProcessingJobDto>("job-2",
				new ProcessingJobDto { Job = job, StartedAt = DateTime.UtcNow })
		]);
		// connectionJobQueue returns "other-queue" → IsJobInQueue will return false
		SetupFakeStorage(processingJobs: processingJobs, connectionJobQueue: "other-queue");
		Dictionary<string, string> matchParameters = new() { { "id", "42" } };

		// Act
		List<string> result = "my-queue".FindAndCancelJobs(nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenScheduledJobBelongsToDifferentQueue_ReturnsEmptyList()
	{
		// Arrange — job matches method + params but lives in a different queue
		Job job = CreateJob(nameof(ITestJobService.RunJob), 42, "test");
		JobList<ScheduledJobDto> scheduledJobs = new(
		[
			new KeyValuePair<string, ScheduledJobDto>("job-3",
				new ScheduledJobDto { Job = job, EnqueueAt = DateTime.UtcNow.AddMinutes(5) })
		]);
		// connectionJobQueue returns "other-queue" → IsJobInQueue will return false
		SetupFakeStorage(scheduledJobs: scheduledJobs, connectionJobQueue: "other-queue");
		Dictionary<string, string> matchParameters = new() { { "id", "42" } };

		// Act
		List<string> result = "my-queue".FindAndCancelJobs(nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenEnqueuedJobDtoIsNull_DoesNotThrow()
	{
		// Arrange — DTO with null Job should be skipped gracefully
		JobList<EnqueuedJobDto> enqueuedJobs = new(
		[
			new KeyValuePair<string, EnqueuedJobDto>("job-1",
				new EnqueuedJobDto { Job = null, State = "Enqueued" })
		]);
		SetupFakeStorage(enqueuedJobs: enqueuedJobs);

		// Act
		List<string> result = "my-queue".FindAndCancelJobs("RunJob", new Dictionary<string, string>());

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public void FindAndCancelJobs_WhenMultipleJobsNoneMatch_ReturnsEmptyList()
	{
		// Arrange
		Job job1 = CreateJob(nameof(ITestJobService.RunJob), 1, "alpha");
		Job job2 = CreateJob(nameof(ITestJobService.RunJob), 2, "beta");
		JobList<EnqueuedJobDto> enqueuedJobs = new(
		[
			new KeyValuePair<string, EnqueuedJobDto>("job-1",
				new EnqueuedJobDto { Job = job1, State = "Enqueued" }),
			new KeyValuePair<string, EnqueuedJobDto>("job-2",
				new EnqueuedJobDto { Job = job2, State = "Enqueued" })
		]);
		SetupFakeStorage(enqueuedJobs: enqueuedJobs);
		Dictionary<string, string> matchParameters = new() { { "id", "99" } }; // matches neither

		// Act
		List<string> result = "my-queue".FindAndCancelJobs(nameof(ITestJobService.RunJob), matchParameters);

		// Assert
		Assert.Empty(result);
	}

	#endregion

	#region FindAndCancelJobs — empty matchParameters matches any job with correct method

	[Fact]
	public void FindAndCancelJobs_WithEmptyMatchParameters_MatchesAnyJobWithCorrectMethodName()
	{
		// Arrange — empty matchParameters should match any job whose method name matches,
		// but BackgroundJob.Delete will return false against a fake storage, so the returned
		// list will still be empty; what we verify is that monitoring API was queried and
		// no exception was raised.
		Job job = CreateJob(nameof(ITestJobService.RunJob), 99, "test");
		JobList<EnqueuedJobDto> enqueuedJobs = new(
		[
			new KeyValuePair<string, EnqueuedJobDto>("job-1",
				new EnqueuedJobDto { Job = job, State = "Enqueued" })
		]);
		var (_, monitoringApi, _) = SetupFakeStorage(enqueuedJobs: enqueuedJobs);

		// Act
		List<string> result = "my-queue".FindAndCancelJobs(nameof(ITestJobService.RunJob), new Dictionary<string, string>());

		// Assert — API was called; delete against fake storage returns false so list is empty
		A.CallTo(() => monitoringApi.EnqueuedJobs("my-queue", 0, int.MaxValue))
			.MustHaveHappenedOnceExactly();
		Assert.IsType<List<string>>(result);
	}

	#endregion

	#region FindAndCancelJobs — connection is acquired for processing and scheduled branches

	[Fact]
	public void FindAndCancelJobs_AcquiresStorageConnectionForProcessingAndScheduledBranches()
	{
		// Arrange
		Job processingJob = CreateJob(nameof(ITestJobService.RunJob), 5, "test");
		Job scheduledJob = CreateJob(nameof(ITestJobService.SingleArg), 7);
		JobList<ProcessingJobDto> processingJobs = new(
		[
			new KeyValuePair<string, ProcessingJobDto>("job-p",
				new ProcessingJobDto { Job = processingJob, StartedAt = DateTime.UtcNow })
		]);
		JobList<ScheduledJobDto> scheduledJobs = new(
		[
			new KeyValuePair<string, ScheduledJobDto>("job-s",
				new ScheduledJobDto { Job = scheduledJob, EnqueueAt = DateTime.UtcNow.AddMinutes(1) })
		]);
		var (storage, _, _) = SetupFakeStorage(processingJobs: processingJobs, scheduledJobs: scheduledJobs, connectionJobQueue: "my-queue");

		// Act
		"my-queue".FindAndCancelJobs("RunJob", new Dictionary<string, string>());

		// Assert — GetConnection() must have been called at least once (one per parallel branch)
		A.CallTo(() => storage.GetConnection()).MustHaveHappened();
	}

	#endregion
}
