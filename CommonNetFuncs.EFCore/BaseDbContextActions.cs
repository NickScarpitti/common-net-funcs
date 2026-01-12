using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace CommonNetFuncs.EFCore;

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="TEntity">Entity <see langword="class"/> to be used with these methods.</typeparam>
/// <typeparam name="TContext">DB Context for the database you with to run these actions against.</typeparam>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for dependency injection.</param>
public partial class BaseDbContextActions<TEntity, TContext>(IServiceProvider serviceProvider) : IBaseDbContextActions<TEntity, TContext> where TEntity : class where TContext : DbContext
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	private static readonly JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
	static readonly ConcurrentDictionary<Type, bool> circularReferencingEntities = new();

	private static readonly ConcurrentDictionary<Type, EntityKeyMetadata> entityKeyCache = new();
	private static readonly ConcurrentDictionary<Type, Func<object, Expression<Func<TEntity, bool>>>> singleKeyExpressionBuilder = new();
	private static readonly ConcurrentDictionary<Type, Func<object[], Expression<Func<TEntity, bool>>>> compositeKeyExpressionBuilder = new();

	private const string Error1LocationTemplate = "{ExceptionLocation} Error1";
	private const string Error2LocationTemplate = "{ExceptionLocation} Error2";
	private const string AddCircularRefTemplate = "Adding {Type} to circularReferencingEntities";
}

/// <summary>
/// Optional configurations for the <see cref="BaseDbContextActions"/> class
/// </summary>
public sealed class FullQueryOptions(bool? splitQueryOverride = null) : NavigationPropertiesOptions
{
	/// <summary>
	/// Optional: Override the database default split query behavior. Only used when running "Full" queries that include navigation properties.
	/// </summary>
	public bool? SplitQueryOverride { get; set; } = splitQueryOverride;
}

public sealed class GlobalFilterOptions
{
	/// <summary>
	/// Optional: If <see langword="true"/>, will disable all global filters for this query.
	/// </summary>
	/// <remarks>If <see langword="false"/>, no filters are disabled unless specified in <see cref="FilterNamesToDisable"/>. <see cref="FilterNamesToDisable"/> having a value will make the code ignore this value.</remarks>
	public bool DisableAllFilters { get; set; }

	/// <summary>
	/// Optional: Used to specify the names of filters that should be excluded from execution.
	/// </summary>
	/// <remarks>If not empty, overwrites <see cref="DisableAllFilters"/>. If <see langword="null"/> or empty, no filters are disabled or all filters are disabled if  <see cref="DisableAllFilters"/> is <see langword="true"/>.</remarks>
	public string[]? FilterNamesToDisable { get; set; }
}

public sealed class GenericPagingModel<T>// where TObj : class
{
	public GenericPagingModel()
	{
		Entities = [];
	}

	public List<T> Entities { get; set; }

	public int TotalRecords { get; set; }
}

/// <summary>
/// Cached metadata for entity primary keys to avoid repeated reflection and EF Core model lookups
/// </summary>
internal sealed class EntityKeyMetadata
{
	/// <summary>
	/// The name of the single primary key property (for single-key entities)
	/// </summary>
	public string? KeyPropertyName { get; set; }

	/// <summary>
	/// The CLR type of the single primary key property
	/// </summary>
	public Type? KeyPropertyType { get; set; }

	/// <summary>
	/// Array of property names for compound primary keys
	/// </summary>
	public string[]? CompositeKeyPropertyNames { get; set; }

	/// <summary>
	/// Array of CLR types for compound primary key properties
	/// </summary>
	public Type[]? CompositeKeyPropertyTypes { get; set; }
}
