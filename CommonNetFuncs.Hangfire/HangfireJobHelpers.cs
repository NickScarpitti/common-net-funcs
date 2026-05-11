using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using NLog;

namespace CommonNetFuncs.Hangfire;

public static class HangfireJobHelpers
{
	private static readonly Logger logger = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Finds and cancels all jobs matching the given queue, method name, and parameters.
	/// Works for both Enqueued (waiting) and Processing (running) jobs.
	/// </summary>
	/// <param name="queueName">The Hangfire queue name, e.g. "reports"</param>
	/// <param name="methodName">The job method name, e.g. "GenerateReport"</param>
	/// <param name="matchParameters">
	///   Key/value pairs to match against job arguments.
	///   Keys are parameter names, values are the expected string representations.
	/// </param>
	/// <returns>List of cancelled job IDs</returns>
	/// <summary>
	/// Strongly-typed overload. Extracts the method name and argument values from the expression,
	/// so callers never have to pass parameter names as strings.
	/// </summary>
	/// <example>
	/// "my-queue".FindAndCancelJobs<IScanLibraryJob>(j => j.ExecuteScan(libraryId, userId, path));
	/// </example>
	public static List<string> FindAndCancelJobs<TJob>(this string queueName, Expression<Action<TJob>> jobExpression)
	{
		if (jobExpression.Body is not MethodCallExpression call)
		{
			throw new ArgumentException("Expression body must be a method call.", nameof(jobExpression));
		}

		string methodName = call.Method.Name;
		ParameterInfo[] paramInfos = call.Method.GetParameters();

		Dictionary<string, string> matchParameters = new(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < call.Arguments.Count; i++)
		{
			object? value = Expression.Lambda(call.Arguments[i]).CompileFast().DynamicInvoke();
			if (value != null)
			{
				matchParameters[paramInfos[i].Name!] = value.ToString()!;
			}
		}

		return queueName.FindAndCancelJobs(methodName, matchParameters);
	}

	/// <summary>
	/// Finds and cancels all jobs matching the given queue, method name, and parameters.
	/// Works for Enqueued (waiting), Processing (running), and Scheduled (delayed) jobs.
	/// </summary>
	/// <param name="queueName">The Hangfire queue name, e.g. "reports"</param>
	/// <param name="methodName">The job method name, e.g. "GenerateReport"</param>
	/// <param name="matchParameters">
	///   Key/value pairs to match against job arguments.
	///   Keys are parameter names, values are the expected string representations.
	/// </param>
	/// <returns>List of cancelled job IDs</returns>
	public static List<string> FindAndCancelJobs(this string queueName, string methodName, Dictionary<string, string> matchParameters)
	{
		IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();
		ConcurrentBag<string> cancelledIds = new();

		Parallel.Invoke(
			// ── Step 1: Cancel ENQUEUED (waiting) jobs ──────────────────
			// EnqueuedJobs is already filtered by queue name — no IsJobInQueue check needed
			() => CancelMatchingJobs(monitoringApi.EnqueuedJobs(queueName, 0, int.MaxValue), dto => dto?.Job, methodName, matchParameters, cancelledIds),

			// ── Step 2: Cancel PROCESSING (running) jobs ─────────────────
			// ProcessingJobs() is global — reuse one connection per branch for all queue checks
			() =>
			{
				using IStorageConnection conn = JobStorage.Current.GetConnection();
				CancelMatchingJobs(monitoringApi.ProcessingJobs(0, int.MaxValue), dto => dto?.Job, methodName, matchParameters, cancelledIds,
					jobId => IsJobInQueue(jobId, queueName, conn));
			},

			// ── Step 3: Also check SCHEDULED jobs (delayed jobs) ─────────
			// ScheduledJobs() is global — reuse one connection per branch for all queue checks
			() =>
			{
				using IStorageConnection conn = JobStorage.Current.GetConnection();
				CancelMatchingJobs(monitoringApi.ScheduledJobs(0, int.MaxValue), dto => dto?.Job, methodName, matchParameters, cancelledIds,
					jobId => IsJobInQueue(jobId, queueName, conn));
			}
		);

		List<string> result = cancelledIds.ToList();

		if (result.Count == 0)
		{
			logger.Warn($"No matching Hangfire jobs found to cancel for queue='{queueName}', method='{methodName}' with parameters: {string.Join(", ", matchParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
		}

		return result;
	}

	/// <summary>
	/// Cancels Hangfire jobs that match the specified method name and parameter values.
	/// </summary>
	/// <typeparam name="TDto">The type of the job DTO.</typeparam>
	/// <param name="jobs">The list of jobs to check.</param>
	/// <param name="getJob">A function to extract the Hangfire Job from the DTO.</param>
	/// <param name="methodName">The name of the method to match.</param>
	/// <param name="matchParameters">A dictionary of parameter names and values to match.</param>
	/// <param name="cancelledIds">A concurrent bag to store the IDs of cancelled jobs.</param>
	/// <param name="queueFilter">An optional filter to check if the job belongs to a specific queue.</param>
	private static void CancelMatchingJobs<TDto>(JobList<TDto> jobs, Func<TDto?, Job?> getJob, string methodName, Dictionary<string, string> matchParameters,
		ConcurrentBag<string> cancelledIds, Func<string, bool>? queueFilter = null) where TDto : class
	{
		foreach (KeyValuePair<string, TDto> entry in jobs)
		{
			if (!IsJobMatch(getJob(entry.Value), methodName, matchParameters))
			{
				continue;
			}

			if (queueFilter != null && !queueFilter(entry.Key))
			{
				continue;
			}

			if (BackgroundJob.Delete(entry.Key))
			{
				cancelledIds.Add(entry.Key);
			}
		}
	}


	/// <summary>
	/// Checks if the given Hangfire Job matches the specified method name and parameter values.
	/// </summary>
	/// <param name="job">The Hangfire Job to check.</param>
	/// <param name="methodName">The name of the method to match.</param>
	/// <param name="matchParameters">A dictionary of parameter names and values to match.</param>
	/// <returns>True if the job matches the method name and parameters, otherwise false.</returns>
	public static bool IsJobMatch(Job? job, string methodName, Dictionary<string, string> matchParameters)
	{
		if (job == null)
		{
			return false;
		}

		if (!job.Method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (matchParameters.Count == 0)
		{
			return true;
		}

		ParameterInfo[] paramInfos = job.Method.GetParameters();
		IReadOnlyList<object> jobArgs = job.Args;

		// Build name→index map once instead of O(n) Array.FindIndex per key
		Dictionary<string, int> paramIndexMap = paramInfos.Select((p, i) => (p.Name!, i)).ToDictionary(x => x.Item1, x => x.i, StringComparer.OrdinalIgnoreCase);

		foreach (KeyValuePair<string, string> kvp in matchParameters)
		{
			if (!paramIndexMap.TryGetValue(kvp.Key, out int paramIndex) || paramIndex >= jobArgs.Count)
			{
				return false;
			}

			string? argValue = jobArgs[paramIndex]?.ToString();
			if (!string.Equals(argValue, kvp.Value, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	///  Checks if the job with the given ID belongs to the specified queue by reading its "Queue" parameter from storage.
	///  If the parameter is missing or any error occurs, it conservatively returns true to avoid false negatives in cancellation.
	/// </summary>
	/// <param name="jobId">The ID of the job to check.</param>
	/// <param name="queueName">The name of the queue to check against.</param>
	/// <returns>True if the job belongs to the specified queue or if any error occurs, otherwise false.</returns>
	public static bool IsJobInQueue(string jobId, string queueName)
	{
		try
		{
			using IStorageConnection connection = JobStorage.Current.GetConnection();
			return IsJobInQueue(jobId, queueName, connection);
		}
		catch
		{
			return true;
		}
	}

	/// <summary>
	/// Checks if the job with the given ID belongs to the specified queue by reading its "Queue" parameter from storage.
	/// If the parameter is missing or any error occurs, it conservatively returns true to avoid false negatives in cancellation.
	/// </summary>
	/// <param name="jobId">The ID of the job to check.</param>
	/// <param name="queueName">The name of the queue to check against.</param>
	/// <param name="connection">The storage connection to use for retrieving job parameters.</param>
	/// <returns>True if the job belongs to the specified queue or if any error occurs, otherwise false.</returns>
	public static bool IsJobInQueue(string jobId, string queueName, IStorageConnection connection)
	{
		try
		{
			string? queueParam = connection.GetJobParameter(jobId, "Queue");
			return queueParam == null || queueParam.Equals(queueName, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return true;
		}
	}
}