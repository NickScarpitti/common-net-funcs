using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.EFCore.Logging;

/// <summary>
/// Filtered <see cref="ILoggerFactory"/> for EF Core that filters out database command logs based on specified string filters.
/// </summary>
/// <param name="innerFactory">Base <see cref="ILoggerFactory"/> factory to build on</param>
/// <param name="stringFilters">A <see cref="Dictionary{TKey, TValue}"/> of filters where TKey is the source name, and TValue is the minimum log level that will result in emitted logs for that source.</param>
public sealed class FilteredEfCoreLoggerFactory(ILoggerFactory innerFactory, IReadOnlyDictionary<string, LogLevel> stringFilters) : ILoggerFactory
{
	private readonly ILoggerFactory innerFactory = innerFactory;

	public ILogger CreateLogger(string categoryName)
	{
		ILogger logger = innerFactory.CreateLogger(categoryName);

		// Filter out database command logs using stringFilters
		KeyValuePair<string, LogLevel>? filter = stringFilters.FirstOrDefault(x => categoryName.Contains(x.Key));
		if (filter != null)
		{
			return new FilteredEfCoreLogger(logger, filter.Value.Value);
		}

		return logger;
	}

	/// <summary>
	/// Adds a provider to the inner <see cref="ILoggerFactory"/>.
	/// </summary>
	/// <param name="provider">The <see cref="ILoggerProvider"/> to add.</param>
	public void AddProvider(ILoggerProvider provider)
	{
		innerFactory.AddProvider(provider);
	}

	/// <summary>
	/// Disposes the <see cref="FilteredEfCoreLoggerFactory"/> and its inner factory.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	bool disposed;

	/// <summary>
	/// Disposes the <see cref="FilteredEfCoreLoggerFactory"/> and its inner factory.
	/// </summary>
	/// <param name="disposing">If <see langword="true"/>, the method is called from Dispose, otherwise from the finalizer.</param>
	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				innerFactory.Dispose();
			}
			disposed = true;
		}
	}

	~FilteredEfCoreLoggerFactory()
	{
		Dispose(false);
	}
}
