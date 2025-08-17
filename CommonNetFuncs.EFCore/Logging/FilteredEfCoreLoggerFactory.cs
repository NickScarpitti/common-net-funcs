using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.EFCore.Logging;

public sealed class FilteredEfCoreLoggerFactory(ILoggerFactory innerFactory, IReadOnlyDictionary<string, LogLevel> stringFilters) : ILoggerFactory
{
    private readonly ILoggerFactory _innerFactory = innerFactory;

    public ILogger CreateLogger(string categoryName)
    {
        ILogger logger = _innerFactory.CreateLogger(categoryName);

        // Filter out database command logs at Info level
        KeyValuePair<string, LogLevel>? filter = stringFilters.FirstOrDefault(x => categoryName.Contains(x.Key));
        if (filter != null)
        {
            return new FilteredEfCoreLogger(logger, filter.Value.Value);
        }

        return logger;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        _innerFactory.AddProvider(provider);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    bool disposed;

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _innerFactory.Dispose();
            }
            disposed = true;
        }
    }

    ~FilteredEfCoreLoggerFactory()
    {
        Dispose(false);
    }
}
