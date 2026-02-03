namespace CommonNetFuncs.Hangfire;

/// <summary>
/// Exception thrown when a Hangfire background job encounters a failure condition.
/// This exception provides context about the job operation and can control retry behavior.
/// Note: Uses JSON serialization (Hangfire default) rather than obsolete binary serialization.
/// </summary>
public sealed class HangfireJobException : Exception
{
	/// <summary>
	/// Gets the name of the operation that failed
	/// </summary>
	public string? OperationName { get; }

	/// <summary>
	/// Gets the ID of the entity being processed when the failure occurred
	/// </summary>
	public int? EntityId { get; }

	/// <summary>
	/// Gets whether this failure should allow Hangfire to retry the job
	/// </summary>
	public bool AllowRetry { get; }

	/// <summary>
	/// Initializes a new instance of the HangfireJobException class (standard exception constructor).
	/// </summary>
	public HangfireJobException()
	{
		AllowRetry = true;
	}

	/// <summary>
	/// Initializes a new instance of the HangfireJobException class with a specified error message (standard exception constructor).
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	public HangfireJobException(string message) : base(message)
	{
		AllowRetry = true;
	}

	/// <summary>
	/// Initializes a new instance of the HangfireJobException class with a specified error message and inner exception (standard exception constructor).
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	public HangfireJobException(string message, Exception? innerException) : base(message, innerException)
	{
		AllowRetry = true;
	}

	/// <summary>
	/// Creates a new HangfireJobException with a specified error message and retry control
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	/// <param name="allowRetry">Whether Hangfire should retry this job</param>
	public HangfireJobException(string message, bool allowRetry) : base(message)
	{
		AllowRetry = allowRetry;
	}

	/// <summary>
	/// Creates a new HangfireJobException with a specified error message and operation name
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	/// <param name="operationName">The name of the operation that failed</param>
	/// <param name="allowRetry">Whether Hangfire should retry this job</param>
	public HangfireJobException(string message, string operationName, bool allowRetry = true) : base(message)
	{
		OperationName = operationName;
		AllowRetry = allowRetry;
	}

	/// <summary>
	/// Creates a new HangfireJobException with full context and retry control
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	/// <param name="operationName">The name of the operation that failed</param>
	/// <param name="entityId">The ID of the entity being processed</param>
	/// <param name="allowRetry">Whether Hangfire should retry this job</param>
	public HangfireJobException(string message, string operationName, int entityId, bool allowRetry = true) : base(message)
	{
		OperationName = operationName;
		EntityId = entityId;
		AllowRetry = allowRetry;
	}

	/// <summary>
	/// Creates a new HangfireJobException with a specified error message, inner exception, and retry control
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	/// <param name="allowRetry">Whether Hangfire should retry this job</param>
	public HangfireJobException(string message, Exception? innerException, bool allowRetry) : base(message, innerException)
	{
		AllowRetry = allowRetry;
	}

	/// <summary>
	/// Creates a new HangfireJobException with full context including inner exception
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception</param>
	/// <param name="operationName">The name of the operation that failed</param>
	/// <param name="entityId">The ID of the entity being processed</param>
	/// <param name="innerException">The exception that is the cause of the current exception</param>
	/// <param name="allowRetry">Whether Hangfire should retry this job</param>
	public HangfireJobException(string message, string operationName, int entityId, Exception? innerException, bool allowRetry = true) : base(message, innerException)
	{
		OperationName = operationName;
		EntityId = entityId;
		AllowRetry = allowRetry;
	}

	/// <summary>
	/// Overrides the message to include operation context
	/// </summary>
	public override string Message
	{
		get
		{
			string baseMessage = base.Message;

			if (!string.IsNullOrEmpty(OperationName))
			{
				baseMessage = $"[Operation: {OperationName}] {baseMessage}";
			}

			if (EntityId.HasValue)
			{
				baseMessage = $"{baseMessage} (Entity ID: {EntityId})";
			}

			if (!AllowRetry)
			{
				baseMessage = $"{baseMessage} [PERMANENT FAILURE - NO RETRY]";
			}

			return baseMessage;
		}
	}

	/// <summary>
	/// Helper method to throw when a required entity is not found
	/// </summary>
	/// <param name="entityType">The type of entity that was not found</param>
	/// <param name="entityId">The ID of the entity that was not found</param>
	/// <param name="operationName">The operation being performed</param>
	/// <param name="allowRetry">Whether the operation should be retried (default: false for missing entities)</param>
	/// <exception cref="HangfireJobException">Always throws</exception>
	public static void ThrowEntityNotFound(string entityType, int entityId, string operationName, bool allowRetry = true)
	{
		throw new HangfireJobException(
			$"{entityType} with ID {entityId} not found",
			operationName,
			entityId,
			allowRetry
		);
	}

	/// <summary>
	/// Helper method to throw when a required entity is not found (for string-based entity IDs)
	/// </summary>
	/// <param name="entityType">The type of entity that was not found</param>
	/// <param name="entityId">The string ID of the entity that was not found</param>
	/// <param name="operationName">The operation being performed</param>
	/// <param name="allowRetry">Whether the operation should be retried (default: false for missing entities)</param>
	/// <exception cref="HangfireJobException">Always throws</exception>
	public static void ThrowEntityNotFound(string entityType, string entityId, string operationName, bool allowRetry = true)
	{
		throw new HangfireJobException(
			$"{entityType} with ID '{entityId}' not found",
			operationName,
			allowRetry
		);
	}

	/// <summary>
	/// Helper method to throw when a required entity is not found (for long-based entity IDs)
	/// </summary>
	/// <param name="entityType">The type of entity that was not found</param>
	/// <param name="entityId">The long ID of the entity that was not found</param>
	/// <param name="operationName">The operation being performed</param>
	/// <param name="allowRetry">Whether the operation should be retried (default: false for missing entities)</param>
	/// <exception cref="HangfireJobException">Always throws</exception>
	public static void ThrowEntityNotFound(string entityType, long entityId, string operationName, bool allowRetry = true)
	{
		throw new HangfireJobException(
			$"{entityType} with ID {entityId} not found",
			operationName,
			allowRetry
		);
	}

	/// <summary>
	/// Helper method to throw when a business rule validation fails
	/// </summary>
	/// <param name="validationMessage">The validation failure message</param>
	/// <param name="operationName">The operation being performed</param>
	/// <param name="entityId">The ID of the entity being validated</param>
	/// <param name="allowRetry">Whether the validation might pass on retry (default: false for validation failures)</param>
	/// <exception cref="HangfireJobException">Always throws</exception>
	public static void ThrowValidationFailed(string validationMessage, string operationName, int entityId, bool allowRetry = true)
	{
		throw new HangfireJobException(
			$"Validation failed: {validationMessage}",
			operationName,
			entityId,
			allowRetry
		);
	}

	/// <summary>
	/// Helper method to throw when a required dependency is unavailable
	/// </summary>
	/// <param name="dependencyName">The name of the unavailable dependency</param>
	/// <param name="operationName">The operation being performed</param>
	/// <param name="allowRetry">Whether the operation should be retried (default: true for transient failures)</param>
	/// <exception cref="HangfireJobException">Always throws</exception>
	public static void ThrowDependencyUnavailable(string dependencyName, string operationName, bool allowRetry = true)
	{
		throw new HangfireJobException(
			$"Required dependency '{dependencyName}' is unavailable",
			operationName,
			0,
			allowRetry
		);
	}
}
