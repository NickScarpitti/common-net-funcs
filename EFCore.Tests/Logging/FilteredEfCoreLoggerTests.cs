using CommonNetFuncs.EFCore.Logging;
using Microsoft.Extensions.Logging;

namespace EFCore.Tests.Logging;

public sealed class FilteredEfCoreLoggerTests
{
	private readonly List<string> _logMessages;
	private readonly ILogger _innerLogger;

	public FilteredEfCoreLoggerTests()
	{
		_logMessages = [];
		ILoggerFactory factory = LoggerFactory.Create(builder =>
		{
			builder.AddProvider(new TestLoggerProvider(_logMessages));
			builder.SetMinimumLevel(LogLevel.Trace);
		});
		_innerLogger = factory.CreateLogger("TestCategory");
	}

	#region IsEnabled Tests

	[Theory]
	[InlineData(LogLevel.Trace, LogLevel.Trace, true)]
	[InlineData(LogLevel.Trace, LogLevel.Debug, true)]
	[InlineData(LogLevel.Debug, LogLevel.Trace, false)]
	[InlineData(LogLevel.Debug, LogLevel.Debug, true)]
	[InlineData(LogLevel.Debug, LogLevel.Information, true)]
	[InlineData(LogLevel.Information, LogLevel.Information, true)]
	[InlineData(LogLevel.Information, LogLevel.Warning, true)]
	[InlineData(LogLevel.Warning, LogLevel.Warning, true)]
	[InlineData(LogLevel.Warning, LogLevel.Error, true)]
	[InlineData(LogLevel.Error, LogLevel.Error, true)]
	[InlineData(LogLevel.Error, LogLevel.Critical, true)]
	[InlineData(LogLevel.Critical, LogLevel.Critical, true)]
	public void IsEnabled_WithVariousLogLevels_ReturnsCorrectValue(LogLevel minLevel, LogLevel testLevel, bool expectedEnabled)
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, minLevel);

		// Act
		bool isEnabled = logger.IsEnabled(testLevel);

		// Assert
		isEnabled.ShouldBe(expectedEnabled);
	}

	[Fact]
	public void IsEnabled_WithLogLevelNone_ReturnsFalseForAll()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.None);

		// Act & Assert
		logger.IsEnabled(LogLevel.Trace).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Debug).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Warning).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Error).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Critical).ShouldBeFalse();
	}

	[Fact]
	public void IsEnabled_BelowMinimumLevel_ReturnsFalse()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Warning);

		// Act & Assert
		logger.IsEnabled(LogLevel.Trace).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Debug).ShouldBeFalse();
		logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
	}

	[Fact]
	public void IsEnabled_AtAndAboveMinimumLevel_ReturnsTrue()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Warning);

		// Act & Assert
		logger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
		logger.IsEnabled(LogLevel.Error).ShouldBeTrue();
		logger.IsEnabled(LogLevel.Critical).ShouldBeTrue();
	}

	#endregion

	#region Log Tests

	[Fact]
	public void Log_WithEnabledLogLevel_PassesToInnerLogger()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act
		logger.LogInformation("Test message");

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Test message"));
	}

	[Fact]
	public void Log_WithDisabledLogLevel_DoesNotPassToInnerLogger()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Warning);

		// Act
		logger.LogInformation("Test message");

		// Assert
		_logMessages.ShouldNotContain(m => m.Contains("Test message"));
	}

	[Fact]
	public void Log_WithException_PassesExceptionToInnerLogger()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Error);
		Exception testException = new("Test exception");

		// Act
		logger.LogError(testException, "Error occurred");

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Error occurred") && m.Contains("Test exception"));
	}

	[Fact]
	public void Log_WithEventId_PassesEventIdToInnerLogger()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);
		EventId eventId = new(123, "TestEvent");

		// Act
		logger.Log(LogLevel.Information, eventId, "Test message", null, (state, ex) => state);

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Test message"));
	}

	[Theory]
	[InlineData(LogLevel.Trace)]
	[InlineData(LogLevel.Debug)]
	[InlineData(LogLevel.Information)]
	[InlineData(LogLevel.Warning)]
	[InlineData(LogLevel.Error)]
	[InlineData(LogLevel.Critical)]
	public void Log_WithAllLogLevels_FiltersCorrectly(LogLevel minLevel)
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, minLevel);

		// Act
		logger.LogTrace("Trace message");
		logger.LogDebug("Debug message");
		logger.LogInformation("Info message");
		logger.LogWarning("Warning message");
		logger.LogError("Error message");
		logger.LogCritical("Critical message");

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Trace message") == (minLevel <= LogLevel.Trace));
		_logMessages.ShouldContain(m => m.Contains("Debug message") == (minLevel <= LogLevel.Debug));
		_logMessages.ShouldContain(m => m.Contains("Info message") == (minLevel <= LogLevel.Information));
		_logMessages.ShouldContain(m => m.Contains("Warning message") == (minLevel <= LogLevel.Warning));
		_logMessages.ShouldContain(m => m.Contains("Error message") == (minLevel <= LogLevel.Error));
		_logMessages.ShouldContain(m => m.Contains("Critical message"));
	}

	[Fact]
	public void Log_WithStructuredLogging_PreservesStructure()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);
		const string template = "User {UserId} logged in from {IpAddress}";

		// Act
		logger.LogInformation(template, 123, "192.168.1.1");

		// Assert
		_logMessages.ShouldContain(m => m.Contains("User") && m.Contains("logged in from"));
	}

	[Fact]
	public void Log_WithNullException_HandlesGracefully()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Error);

		// Act & Assert
		Should.NotThrow(() => logger.LogError(null, "Error message"));
		_logMessages.ShouldContain(m => m.Contains("Error message"));
	}

	[Fact]
	public void Log_WithEmptyMessage_HandlesGracefully()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act & Assert
		Should.NotThrow(() => logger.LogInformation(string.Empty));
	}

	[Fact]
	public void Log_WithNullMessage_HandlesGracefully()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act & Assert
		Should.NotThrow(() => logger.LogInformation(null!));
	}

	#endregion

	#region BeginScope Tests

	[Fact]
	public void BeginScope_CreatesScope()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act
		IDisposable? scope = logger.BeginScope("TestScope");

		// Assert
		scope.ShouldNotBeNull();
	}

	[Fact]
	public void BeginScope_WithString_PassesToInnerLogger()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act & Assert
		Should.NotThrow(() =>
		{
			using IDisposable? scope = logger.BeginScope("TestScope");
			logger.LogInformation("Message in scope");
		});

		_logMessages.ShouldContain(m => m.Contains("Message in scope"));
	}

	[Fact]
	public void BeginScope_WithDictionary_PassesToInnerLogger()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);
		Dictionary<string, object> scopeData = new()
		{
			["UserId"] = 123,
			["Action"] = "Login"
		};

		// Act & Assert
		Should.NotThrow(() =>
		{
			using IDisposable? scope = logger.BeginScope(scopeData);
			logger.LogInformation("Scoped message");
		});

		_logMessages.ShouldContain(m => m.Contains("Scoped message"));
	}

	[Fact]
	public void BeginScope_WithNestedScopes_HandlesCorrectly()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act & Assert
		Should.NotThrow(() =>
		{
			using (logger.BeginScope("OuterScope"))
			{
				using (logger.BeginScope("InnerScope"))
				{
					logger.LogInformation("Nested message");
				}
			}
		});

		_logMessages.ShouldContain(m => m.Contains("Nested message"));
	}

	[Fact]
	public void BeginScope_DisposesCorrectly()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);

		// Act & Assert
		Should.NotThrow(() =>
		{
			IDisposable? scope = logger.BeginScope("TestScope");
			scope?.Dispose();
			scope?.Dispose(); // Should not throw on second dispose
		});
	}

	#endregion

	#region Integration Tests

	[Fact]
	public void IntegrationTest_SimulateEfCoreLogging()
	{
		// Arrange - Simulate EF Core database command logging
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Warning);

		// Act - Simulate various EF Core log messages
		logger.Log(LogLevel.Debug, new EventId(20100), "Executing SQL command", null, (state, ex) => state.ToString()!);
		logger.Log(LogLevel.Information, new EventId(20101), "Executed SQL in 50ms", null, (state, ex) => state.ToString()!);
		logger.Log(LogLevel.Warning, new EventId(20500), "Query took longer than expected: 2000ms", null, (state, ex) => state.ToString()!);
		logger.Log(LogLevel.Error, new EventId(20102), "Database error occurred", new InvalidOperationException("Connection lost"), (state, ex) => state.ToString()!);

		// Assert
		_logMessages.ShouldNotContain(m => m.Contains("Executing SQL command"));
		_logMessages.ShouldNotContain(m => m.Contains("Executed SQL in 50ms"));
		_logMessages.ShouldContain(m => m.Contains("Query took longer than expected"));
		_logMessages.ShouldContain(m => m.Contains("Database error occurred"));
	}

	[Fact]
	public void IntegrationTest_WithMultipleFilters()
	{
		// Arrange
		FilteredEfCoreLogger warningLogger = new(_innerLogger, LogLevel.Warning);
		FilteredEfCoreLogger errorLogger = new(_innerLogger, LogLevel.Error);

		// Act
		warningLogger.LogInformation("Info from warning logger");
		warningLogger.LogWarning("Warning from warning logger");
		errorLogger.LogWarning("Warning from error logger");
		errorLogger.LogError("Error from error logger");

		// Assert
		_logMessages.ShouldNotContain(m => m.Contains("Info from warning logger"));
		_logMessages.ShouldContain(m => m.Contains("Warning from warning logger"));
		_logMessages.ShouldNotContain(m => m.Contains("Warning from error logger"));
		_logMessages.ShouldContain(m => m.Contains("Error from error logger"));
	}

	[Fact]
	public void IntegrationTest_WithCustomFormatter()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Information);
		const string customState = "CustomState";

		// Act
		logger.Log(
			LogLevel.Information,
			new EventId(1),
			customState,
			null,
			(state, ex) => $"Formatted: {state}"
		);

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Formatted: CustomState"));
	}

	#endregion

	#region Performance Tests

	[Fact]
	public void Log_WithHighVolume_HandlesCorrectly()
	{
		// Arrange
		FilteredEfCoreLogger logger = new(_innerLogger, LogLevel.Warning);
		const int messageCount = 1000;

		// Act
		for (int i = 0; i < messageCount; i++)
		{
			logger.LogInformation($"Info message {i}");
			logger.LogWarning($"Warning message {i}");
		}

		// Assert
		// Only warning messages should be logged
		_logMessages.Count(m => m.Contains("Warning message")).ShouldBe(messageCount);
		_logMessages.ShouldNotContain(m => m.Contains("Info message"));
	}

	#endregion
}
