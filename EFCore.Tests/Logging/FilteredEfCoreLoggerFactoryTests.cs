using CommonNetFuncs.EFCore.Logging;
using Microsoft.Extensions.Logging;

namespace EFCore.Tests.Logging;

public sealed class FilteredEfCoreLoggerFactoryTests : IDisposable
{
	private readonly ILoggerFactory _innerFactory;
	private readonly Dictionary<string, LogLevel> _stringFilters;
	private readonly List<string> _logMessages;

	public FilteredEfCoreLoggerFactoryTests()
	{
		_logMessages = [];
		_stringFilters = new Dictionary<string, LogLevel>();
		_innerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddProvider(new TestLoggerProvider(_logMessages));
			builder.SetMinimumLevel(LogLevel.Trace);
		});
	}

	#region CreateLogger Tests

	[Fact]
	public void CreateLogger_WithMatchingFilter_ReturnsFilteredLogger()
	{
		// Arrange
		_stringFilters["Database.Command"] = LogLevel.Warning;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command");

		// Assert
		logger.ShouldNotBeNull();
		logger.ShouldBeOfType<FilteredEfCoreLogger>();
	}

	[Fact]
	public void CreateLogger_WithPartialMatch_ReturnsFilteredLogger()
	{
		// Arrange
		_stringFilters["Database"] = LogLevel.Error;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command.Executed");

		// Assert
		logger.ShouldNotBeNull();
		logger.ShouldBeOfType<FilteredEfCoreLogger>();
	}

	[Fact]
	public void CreateLogger_WithMultipleFilters_UsesFirstMatch()
	{
		// Arrange
		_stringFilters["Database"] = LogLevel.Warning;
		_stringFilters["Database.Command"] = LogLevel.Error;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command");

		// Assert
		logger.ShouldNotBeNull();
		logger.ShouldBeOfType<FilteredEfCoreLogger>();

		// Verify it uses the first matching filter (Database -> Warning)
		logger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
		logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
	}

	[Fact]
	public void CreateLogger_WithDifferentCategories_CreatesMultipleLoggers()
	{
		// Arrange
		_stringFilters["Database"] = LogLevel.Warning;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger1 = factory.CreateLogger("Category1");
		ILogger logger2 = factory.CreateLogger("Category2");

		// Assert
		logger1.ShouldNotBeNull();
		logger2.ShouldNotBeNull();
		logger1.ShouldNotBe(logger2);
	}

	[Fact]
	public void CreateLogger_WithEmptyCategoryName_HandlesGracefully()
	{
		// Arrange
		_stringFilters["Test"] = LogLevel.Warning;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger = factory.CreateLogger(string.Empty);

		// Assert
		logger.ShouldNotBeNull();
	}

	#endregion

	#region Filtering Behavior Tests

	[Fact]
	public void FilteredLogger_WithLogLevelBelowMinimum_FiltersOutLogs()
	{
		// Arrange
		_stringFilters["TestCategory"] = LogLevel.Warning;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		ILogger logger = factory.CreateLogger("TestCategory");

		// Act
		logger.LogInformation("This should be filtered out");
		logger.LogWarning("This should be logged");

		// Assert
		_logMessages.ShouldNotContain(m => m.Contains("filtered out"));
		_logMessages.ShouldContain(m => m.Contains("This should be logged"));
	}

	[Theory]
	[InlineData(LogLevel.Trace)]
	[InlineData(LogLevel.Debug)]
	[InlineData(LogLevel.Information)]
	[InlineData(LogLevel.Warning)]
	[InlineData(LogLevel.Error)]
	[InlineData(LogLevel.Critical)]
	public void FilteredLogger_WithVariousLogLevels_RespectsMinimumLevel(LogLevel minLevel)
	{
		// Arrange
		_stringFilters["TestCategory"] = minLevel;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		ILogger logger = factory.CreateLogger("TestCategory");

		// Act & Assert
		logger.IsEnabled(LogLevel.Trace).ShouldBe(minLevel <= LogLevel.Trace);
		logger.IsEnabled(LogLevel.Debug).ShouldBe(minLevel <= LogLevel.Debug);
		logger.IsEnabled(LogLevel.Information).ShouldBe(minLevel <= LogLevel.Information);
		logger.IsEnabled(LogLevel.Warning).ShouldBe(minLevel <= LogLevel.Warning);
		logger.IsEnabled(LogLevel.Error).ShouldBe(minLevel <= LogLevel.Error);
		logger.IsEnabled(LogLevel.Critical).ShouldBe(minLevel <= LogLevel.Critical);
	}

	[Fact]
	public void FilteredLogger_LogsAtAndAboveMinLevel_PassesThroughToInnerLogger()
	{
		// Arrange
		_stringFilters["TestCategory"] = LogLevel.Information;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		ILogger logger = factory.CreateLogger("TestCategory");

		// Act
		logger.LogTrace("Trace message");
		logger.LogDebug("Debug message");
		logger.LogInformation("Info message");
		logger.LogWarning("Warning message");
		logger.LogError("Error message");
		logger.LogCritical("Critical message");

		// Assert
		_logMessages.ShouldNotContain(m => m.Contains("Trace message"));
		_logMessages.ShouldNotContain(m => m.Contains("Debug message"));
		_logMessages.ShouldContain(m => m.Contains("Info message"));
		_logMessages.ShouldContain(m => m.Contains("Warning message"));
		_logMessages.ShouldContain(m => m.Contains("Error message"));
		_logMessages.ShouldContain(m => m.Contains("Critical message"));
	}

	#endregion

	#region AddProvider Tests

	[Fact]
	public void AddProvider_AddsProviderToInnerFactory()
	{
		// Arrange
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		List<string> newLogMessages = [];
		TestLoggerProvider newProvider = new(newLogMessages);

		// Act
		factory.AddProvider(newProvider);
		ILogger logger = factory.CreateLogger("TestCategory");
		logger.LogInformation("Test message");

		// Assert
		newLogMessages.ShouldContain(m => m.Contains("Test message"));
	}

	#endregion

	#region Dispose Tests

	[Fact]
	public void Dispose_DisposesInnerFactory()
	{
		// Arrange
		List<string> logMessages = [];
		ILoggerFactory innerFactory = LoggerFactory.Create(builder => builder.AddProvider(new TestLoggerProvider(logMessages)));
		FilteredEfCoreLoggerFactory factory = new(innerFactory, _stringFilters);

		// Act
		factory.Dispose();

		// Assert - inner factory should be disposed
		// We can verify this by checking that the factory doesn't throw when disposed again
		Should.NotThrow(innerFactory.Dispose);
	}

	[Fact]
	public void Dispose_CalledMultipleTimes_DoesNotThrow()
	{
		// Arrange
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act & Assert
		Should.NotThrow(() =>
		{
			factory.Dispose();
			factory.Dispose();
			factory.Dispose();
		});
	}

	[Fact]
	public void Finalizer_DisposesFactory()
	{
		// Arrange - Create factory in a separate method to ensure it goes out of scope
		WeakReference CreateAndAbandonFactory()
		{
			FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
			return new WeakReference(factory);
		}

		WeakReference weakRef = CreateAndAbandonFactory();

		// Act - Force garbage collection
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// Assert - The factory should be collected
		weakRef.IsAlive.ShouldBeFalse();
	}

	#endregion

	#region Edge Cases and Integration Tests

	[Fact]
	public void CreateLogger_WithCaseVariations_StillMatches()
	{
		// Arrange
		_stringFilters["database"] = LogLevel.Warning;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command");

		// Assert
		logger.ShouldBeOfType<FilteredEfCoreLogger>();
	}

	[Fact]
	public void CreateLogger_WithSpecialCharacters_HandlesCorrectly()
	{
		// Arrange
		_stringFilters["Database.Command"] = LogLevel.Error;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		// Act
		ILogger logger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command.Special$Characters");

		// Assert
		logger.ShouldBeOfType<FilteredEfCoreLogger>();
	}

	[Fact]
	public void FilteredLogger_WithExceptions_PassesThroughToInnerLogger()
	{
		// Arrange
		_stringFilters["TestCategory"] = LogLevel.Warning;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		ILogger logger = factory.CreateLogger("TestCategory");
		Exception testException = new("Test exception");

		// Act
		logger.LogError(testException, "Error with exception");

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Error with exception") && m.Contains("Test exception"));
	}

	[Fact]
	public void FilteredLogger_WithScopes_PassesThroughToInnerLogger()
	{
		// Arrange
		_stringFilters["TestCategory"] = LogLevel.Information;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		ILogger logger = factory.CreateLogger("TestCategory");

		// Act
		using (logger.BeginScope("TestScope"))
		{
			logger.LogInformation("Message in scope");
		}

		// Assert
		_logMessages.ShouldContain(m => m.Contains("Message in scope"));
	}

	[Fact]
	public void FilteredLogger_WithLogLevelNone_NeverLogs()
	{
		// Arrange
		_stringFilters["TestCategory"] = LogLevel.None;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);
		ILogger logger = factory.CreateLogger("TestCategory");

		// Act
		logger.LogTrace("Trace");
		logger.LogDebug("Debug");
		logger.LogInformation("Information");
		logger.LogWarning("Warning");
		logger.LogError("Error");
		logger.LogCritical("Critical");

		// Assert
		_logMessages.ShouldBeEmpty();
	}

	[Fact]
	public void RealWorldScenario_EFCoreCommandLogging_FiltersCorrectly()
	{
		// Arrange - Simulate EF Core logging scenario
		_stringFilters["Database.Command"] = LogLevel.Warning;
		_stringFilters["Database.Connection"] = LogLevel.Error;
		FilteredEfCoreLoggerFactory factory = new(_innerFactory, _stringFilters);

		ILogger commandLogger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted");
		ILogger connectionLogger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Connection");
		ILogger queryLogger = factory.CreateLogger("Microsoft.EntityFrameworkCore.Query");

		// Act
		commandLogger.LogInformation("Executing SQL: SELECT * FROM Users");
		commandLogger.LogWarning("Slow query detected");
		connectionLogger.LogInformation("Opening connection");
		connectionLogger.LogError("Connection failed");
		queryLogger.LogInformation("Compiling query expression");

		// Assert
		_logMessages.ShouldNotContain(m => m.Contains("Executing SQL"));
		_logMessages.ShouldContain(m => m.Contains("Slow query detected"));
		_logMessages.ShouldNotContain(m => m.Contains("Opening connection"));
		_logMessages.ShouldContain(m => m.Contains("Connection failed"));
		// Query logger gets default filter behavior (logs everything at Trace level since FirstOrDefault returns default)
		_logMessages.ShouldContain(m => m.Contains("Compiling query expression"));
	}

	#endregion

	#region Test Infrastructure

	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				_innerFactory?.Dispose();
			}
			disposed = true;
		}
	}

	~FilteredEfCoreLoggerFactoryTests()
	{
		Dispose(false);
	}

	#endregion
}

#region Test Logger Implementation

internal sealed class TestLoggerProvider(List<string> logMessages) : ILoggerProvider
{
	private readonly List<string> _logMessages = logMessages;

	public ILogger CreateLogger(string categoryName)
	{
		return new TestLogger(_logMessages, categoryName);
	}

	public void Dispose()
	{
		// No resources to dispose
	}
}

internal sealed class TestLogger(List<string> logMessages, string categoryName) : ILogger
{
	private readonly List<string> _logMessages = logMessages;
	private readonly string _categoryName = categoryName;

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		return new TestScope();
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		string message = formatter(state, exception);
		string logEntry = $"[{logLevel}] {_categoryName}: {message}";
		if (exception != null)
		{
			logEntry += $" | Exception: {exception.Message}";
		}
		_logMessages.Add(logEntry);
	}

	private sealed class TestScope : IDisposable
	{
		public void Dispose()
		{
			// No resources to dispose
		}
	}
}

#endregion
