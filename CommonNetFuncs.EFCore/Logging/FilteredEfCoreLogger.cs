using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.EFCore.Logging;

public sealed class FilteredEfCoreLogger(ILogger innerLogger, LogLevel minLogLevel) : ILogger
{
	private readonly ILogger innerLogger = innerLogger;
	private readonly LogLevel minLogLevel = minLogLevel;

	/// <summary>
	/// Begins a logical operation scope for the inner <see cref="ILogger"/>.
	/// </summary>
	/// <param name="state"><see cref="TState"/> that contains the state for the scope.</param>"/>
	/// <typeparam name="TState">The type of the state object to be used.</typeparam>
	/// <returns><see cref="IDisposable"/> that can be used to end the scope.</returns>"/>
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return innerLogger.BeginScope(state);
	}

	/// <summary>
	/// Determines whether logging is enabled for the specified log level.
	/// </summary>
	/// <remarks>Logging is considered enabled if the specified <paramref name="logLevel"/> is greater than or
	/// equal to the minimum log level and the underlying logger also indicates that logging is enabled for the specified level.</remarks>
	/// <param name="logLevel">The log level to check.</param>
	/// <returns><see langword="true"/> if logging is enabled for the specified <paramref name="logLevel"/>, otherwise <see langword="false"/>.</returns>
	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel >= minLogLevel && innerLogger.IsEnabled(logLevel);
	}

	/// <summary>
	/// Logs a message with the specified log level, event ID, state, exception, and formatter.
	/// </summary>
	/// <remarks>The log message is only written if the specified <paramref name="logLevel"/> is enabled. Use this method to log structured data and exceptions with a custom formatter.</remarks>
	/// <typeparam name="TState">The type of the state object to be logged.</typeparam>
	/// <param name="logLevel">The severity level of the log message.</param>
	/// <param name="eventId">The identifier for the event being logged.</param>
	/// <param name="state">The state object that contains the log message or additional context.</param>
	/// <param name="exception">The exception related to the log entry, or <see langword="null"/> if no exception is associated.</param>
	/// <param name="formatter">A function that formats the <paramref name="state"/> and <paramref name="exception"/> into a log message string.</param>
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (IsEnabled(logLevel))
		{
			innerLogger.Log(logLevel, eventId, state, exception, formatter);
		}
	}
}
