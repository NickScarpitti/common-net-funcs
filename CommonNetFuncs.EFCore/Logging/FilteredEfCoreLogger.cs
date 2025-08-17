using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.EFCore.Logging;

public sealed class FilteredEfCoreLogger(ILogger innerLogger, LogLevel minLogLevel) : ILogger
{
    private readonly ILogger _innerLogger = innerLogger;
    private readonly LogLevel _minLogLevel = minLogLevel;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLogLevel && _innerLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
