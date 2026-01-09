using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;

using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.EFCore;

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

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="T">Entity <see langword="class"/> to be used with these methods.</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against.</typeparam>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for dependency injection.</param>
public class BaseDbContextActions<T, UT>(IServiceProvider serviceProvider) : IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	private static readonly JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
	static readonly ConcurrentDictionary<Type, bool> circularReferencingEntities = new();

	private static readonly ConcurrentDictionary<Type, EntityKeyMetadata> entityKeyCache = new();
	private static readonly ConcurrentDictionary<Type, Func<object, Expression<Func<T, bool>>>> singleKeyExpressionBuilder = new();
	private static readonly ConcurrentDictionary<Type, Func<object[], Expression<Func<T, bool>>>> compositeKeyExpressionBuilder = new();

	private const string Error1LocationTemplate = "{ExceptionLocation} Error1";
	private const string Error2LocationTemplate = "{ExceptionLocation} Error2";
	private const string AddCircularRefTemplate = "Adding {Type} to circularReferencingEntities";

	#region Read

	#region GetByKey

	/// <summary>
	/// Get individual record by the single field primary key, with or without navigation properties
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: Used only when running "Full" query. If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	public Task<T?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken) :
			GetByKeyFull(primaryKey, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Get individual record by the single field primary key.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="globalFilterOptions">Optional: Options for controlling global query filters. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	public async Task<T?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			// If we need to disable filters, we can't use FindAsync - need to build a query
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				// Get or create an expression builder for this entity type
				Func<object, Expression<Func<T, bool>>> expressionBuilder = singleKeyExpressionBuilder.GetOrAdd(typeof(T), type =>
				{
					// Get cached entity metadata or retrieve and cache it
					EntityKeyMetadata keyMetadata = entityKeyCache.GetOrAdd(type, t =>
					{
						IEntityType? entityType = context.Model.FindEntityType(t);
						IKey? primaryKey_Property = entityType?.FindPrimaryKey();

						if (primaryKey_Property == null || primaryKey_Property.Properties.Count != 1)
						{
							throw new InvalidOperationException($"Entity {t.Name} does not have a single-field primary key. Use the array overload instead.");
						}

						return new EntityKeyMetadata
						{
							KeyPropertyName = primaryKey_Property.Properties[0].Name,
							KeyPropertyType = primaryKey_Property.Properties[0].ClrType
						};
					});

					// Return a function that builds an expression tree with the actual key value
					// This function is cached, but the expression it creates includes the specific key value
					return (keyValue) =>
					{
						ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
						MemberExpression property = Expression.Property(parameter, keyMetadata.KeyPropertyName!);
						ConstantExpression constant = Expression.Constant(keyValue, keyMetadata.KeyPropertyType!);
						BinaryExpression equality = Expression.Equal(property, constant);
						return Expression.Lambda<Func<T, bool>>(equality, parameter);
					};
				});

				// Build the expression with the actual primary key value
				Expression<Func<T, bool>> lambda = expressionBuilder(primaryKey);

				IQueryable<T> query = context.Set<T>().Where(lambda);

				// Apply filter disabling
				if (globalFilterOptions.FilterNamesToDisable?.Length > 0)
				{
					query = query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else if (globalFilterOptions.DisableAllFilters)
				{
					query = query.IgnoreQueryFilters();
				}

				model = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// Use the more efficient FindAsync when filters don't need to be disabled
				model = await context.Set<T>().FindAsync([primaryKey], cancellationToken: cancellationToken).ConfigureAwait(true);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Get individual record by the single field primary key with or without navigation properties
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: Used only when running "Full" query. If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	public Task<T?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken) : GetByKeyFull(primaryKey, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Get individual record by a compound primary key.
	/// The values in the primaryKey array need to be ordered in the same order they are declared in AppDbContext
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="globalFilterOptions">Optional: Options for controlling global query filters. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	public async Task<T?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			// If we need to disable filters, we can't use FindAsync - need to build a query
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				// Get or create an expression builder for compound keys
				Func<object[], Expression<Func<T, bool>>> expressionBuilder = compositeKeyExpressionBuilder.GetOrAdd(typeof(T), type =>
				{
					// Get the primary key property names from cache or retrieve and cache them
					EntityKeyMetadata keyMetadata = entityKeyCache.GetOrAdd(type, t =>
					{
						IEntityType? entityType = context.Model.FindEntityType(t);
						IReadOnlyList<IProperty>? primaryKeyProperties = entityType?.FindPrimaryKey()?.Properties;

						return primaryKeyProperties == null
							? throw new InvalidOperationException($"Entity {t.Name} does not have a primary key.")
							: new EntityKeyMetadata
							{
								CompositeKeyPropertyNames = primaryKeyProperties.Select(p => p.Name).ToArray(),
								CompositeKeyPropertyTypes = primaryKeyProperties.Select(p => p.ClrType).ToArray()
							};
					});

					// Return a function that builds an expression tree with the actual key values
					return (keyValues) =>
					{
						if (keyMetadata.CompositeKeyPropertyNames!.Length != keyValues.Length)
						{
							throw new InvalidOperationException($"Primary key count mismatch for entity {type.Name}.");
						}

						ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
						Expression? combinedExpression = null;

						for (int i = 0; i < keyMetadata.CompositeKeyPropertyNames.Length; i++)
						{
							MemberExpression property = Expression.Property(parameter, keyMetadata.CompositeKeyPropertyNames[i]);
							ConstantExpression constant = Expression.Constant(keyValues[i], keyMetadata.CompositeKeyPropertyTypes![i]);
							BinaryExpression equality = Expression.Equal(property, constant);

							combinedExpression = combinedExpression == null ? equality : Expression.AndAlso(combinedExpression, equality);
						}

						if (combinedExpression == null)
						{
							throw new InvalidOperationException($"Could not build primary key expression for entity {type.Name}.");
						}

						return Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
					};
				});

				// Build the expression with the actual primary key values
				Expression<Func<T, bool>> lambda = expressionBuilder(primaryKey);

				IQueryable<T> query = context.Set<T>().Where(lambda);

				// Apply filter disabling
				if (globalFilterOptions.FilterNamesToDisable?.Length > 0)
				{
					query = query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else if (globalFilterOptions.DisableAllFilters)
				{
					query = query.IgnoreQueryFilters();
				}

				model = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// Use the more efficient FindAsync when filters don't need to be disabled
				model = await context.Set<T>().FindAsync(primaryKey, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Get individual record by the primary key with all navigation properties.
	/// If using a compound primary key, use an object of the same <see langword="class"/> to be returned with the primary key fields populated.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	public async Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			model = await GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken).ConfigureAwait(false);
			if (model != null)
			{
				model = fullQueryOptions.SplitQueryOverride switch
				{
					null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
						context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
						context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
						context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
				};
			}
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken).ConfigureAwait(false);
					if (model != null)
					{
						model = fullQueryOptions.SplitQueryOverride switch
						{
							null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
						};
					}
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		//Microsoft.EntityFrameworkCore.Query.NavigationBaseIncludeIgnored

		return model;
	}

	/// <summary>
	/// Get individual record by the primary key with all navigation properties.
	/// If using a compound primary key, use an object of the same <see langword="class"/> to be returned with the primary key fields populated.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	public async Task<T?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			model = await GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken).ConfigureAwait(false);
			if (model != null)
			{
				model = fullQueryOptions.SplitQueryOverride switch
				{
					null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
						context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
						context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
						context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
				};
			}
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken).ConfigureAwait(false);
					if (model != null)
					{
						model = fullQueryOptions.SplitQueryOverride switch
						{
							null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
						};
					}
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		return model;
	}

	#endregion

	#region GetAll

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public Task<List<T>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAll(queryTimeout, trackEntities, globalFilterOptions, cancellationToken) : GetAllFull(queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public async Task<List<T>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T>? model = null;
		try
		{
			model = await GetQueryAll(queryTimeout, trackEntities, globalFilterOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public Task<List<T2>?> GetAll<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAll(selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetAllFull(selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table and transforms them into the type <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	public async Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T2>? model = null;
		try
		{
			model = await GetQueryAll(selectExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public IAsyncEnumerable<T>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAllStreaming(queryTimeout, trackEntities, globalFilterOptions, cancellationToken) : GetAllFullStreaming(queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public async IAsyncEnumerable<T>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryAll(queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public IAsyncEnumerable<T2>? GetAllStreaming<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAllStreaming(selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetAllFullStreaming(selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	public async IAsyncEnumerable<T2>? GetAllStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T2>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryAll(selectExpression, queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T2 enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public IQueryable<T> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !trackEntities ? context.Set<T>().AsNoTracking() : context.Set<T>();
		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			return (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query;
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	public IQueryable<T2> GetQueryAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !trackEntities ? context.Set<T>().AsNoTracking() : context.Set<T>();
		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query.Select(selectExpression);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public async Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T>? model = null;
		try
		{
			model = await GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryAllFull(queryTimeout, true, fullQueryOptions: fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="T2"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	public async Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T2>? model = null;
		try
		{
			model = await GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions, globalFilterOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryAllFull(selectExpression, queryTimeout, true, globalFilterOptions: globalFilterOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public async IAsyncEnumerable<T>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryAllFull(queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="T2"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	public async IAsyncEnumerable<T2>? GetAllFullStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IQueryable<T2> query = GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
		IAsyncEnumerable<T2>? enumeratedReader = null;
		try
		{
			enumeratedReader = query.AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					query = GetQueryAllFull(selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
					enumeratedReader = query.AsAsyncEnumerable();
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T2 enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	public IQueryable<T> GetQueryAllFull(TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
				true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			return (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query;
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table and transforms them <typeparamref name="T2"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	public IQueryable<T2> GetQueryAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
				true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query.Select(selectExpression);
	}

	#endregion

	#region GetWithFilter

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public Task<List<T>?> GetWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilter(whereExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T>? model = null;
		try
		{
			model = await GetQueryWithFilter(whereExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="T2"/> class using the select expression
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
	public Task<List<T2>?> GetWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T2>? model = null;
		try
		{
			model = await GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	public IServiceProvider ServiceProvider { get; set; } = serviceProvider;

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public IAsyncEnumerable<T>? GetWithFilterStreaming(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilterStreaming(whereExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetWithFilterFullStreaming(whereExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public async IAsyncEnumerable<T>? GetWithFilterStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryWithFilter(whereExpression, queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="T2"/> class using the select expression
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
	public IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public async IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T2>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T2 enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<T> GetQueryWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking() : context.Set<T>().Where(whereExpression);
		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			return (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query;
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<T2> GetQueryWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking() : context.Set<T>().Where(whereExpression);
		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query.Select(selectExpression).Distinct();
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T>? model = null;
		try
		{
			model = await GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryWithFilterFull(whereExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions)
						.ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="T2"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
	public async Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		List<T2>? model = null;
		try
		{
			model = await GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions)
				.ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions)
						.ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public async IAsyncEnumerable<T>? GetWithFilterFullStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryWithFilterFull(whereExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="T2"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
	public async IAsyncEnumerable<T2>? GetWithFilterFullStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T2>? enumeratedReader = null;

		try
		{
			enumeratedReader = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T2 enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			return (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query;
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="T2"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
	public IQueryable<T2> GetQueryWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
		bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query.Select(selectExpression).Distinct();
	}

	#endregion

	#region GetNavigationWithFilter

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> with or without its navigation properties using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> class with the select expression.</returns>
	public Task<List<T>?> GetNavigationWithFilter<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		return !full ? GetNavigationWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions: globalFilterOptions, cancellationToken: cancellationToken) :
			GetNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
	public async Task<List<T>?> GetNavigationWithFilter<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		List<T>? model = null;
		try
		{
			model = await GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions)
						.ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T2), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> with or without its navigation properties using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
	public IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		return !full ? GetNavigationWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions: globalFilterOptions, cancellationToken: cancellationToken) :
			GetNavigationWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
	public async IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T2 : class
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T2), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
	public async Task<List<T>?> GetNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		List<T>? model = null;
		try
		{
			IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
			await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
			model = fullQueryOptions.SplitQueryOverride switch
			{
				//Need to add in navigation properties of the output type since they are not kept in the original query
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
					await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
					await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
					await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
				logger.Warn(AddCircularRefTemplate, typeof(T2).Name);
				circularReferencingEntities.TryAdd(typeof(T2), true);
				try
				{
					await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
					model = fullQueryOptions.SplitQueryOverride switch
					{
						//Need to add in navigation properties of the output type since they are not kept in the original query
						null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
							await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
							await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
						true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
							await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
							await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
						_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
							await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
							await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
					};
				}
				catch (InvalidOperationException ioEx2) //Error could be caused by navigation properties of the output type, so need to try that as well
				{
					if (ioEx2.HResult == -2146233079)
					{
						try
						{
							logger.Warn(AddCircularRefTemplate, typeof(T).Name);
							circularReferencingEntities.TryAdd(typeof(T), true);
							await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
							model = fullQueryOptions.SplitQueryOverride switch
							{
								null => await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
								true => await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
								_ => await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
							};
						}
						catch (Exception ex2)
						{
							logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
							logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
						}
					}
					else
					{
						logger.Error(ioEx2, ErrorLocationTemplate, ioEx2.GetLocationOfException());
					}
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
	public async IAsyncEnumerable<T>? GetNavigationWithFilterFullStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T2 : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
			await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
			enumeratedReader = fullQueryOptions.SplitQueryOverride switch
			{
				//Need to add in navigation properties of the output type since they are not kept in the original query
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
					query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
					query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
					query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
				logger.Warn(AddCircularRefTemplate, typeof(T2).Name);
				circularReferencingEntities.TryAdd(typeof(T2), true);
				try
				{
					await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
					enumeratedReader = fullQueryOptions.SplitQueryOverride switch
					{
						//Need to add in navigation properties of the output type since they are not kept in the original query
						null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
							query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
							query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
						true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
							query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
							query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
						_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
							query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
							query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
					};
				}
				catch (InvalidOperationException ioEx2) //Error could be caused by navigation properties of the output type, so need to try that as well
				{
					if (ioEx2.HResult == -2146233079)
					{
						try
						{
							logger.Warn(AddCircularRefTemplate, typeof(T).Name);
							circularReferencingEntities.TryAdd(typeof(T), true);
							await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
							enumeratedReader = fullQueryOptions.SplitQueryOverride switch
							{
								null => query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
								true => query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
								_ => query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
							};
						}
						catch (Exception ex2)
						{
							logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
							logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
						}
					}
					else
					{
						logger.Error(ioEx2, ErrorLocationTemplate, ioEx2.GetLocationOfException());
					}
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
	public IQueryable<T> GetQueryNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
		bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null) where T2 : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
					context.Set<T2>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
					context.Set<T2>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
					context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
					context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
					context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
					context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T2>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				true => context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				_ => context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			return (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query;
	}

	#endregion

	#region GetWithPagingFilter

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null,
		int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		return !full ? GetWithPagingFilter(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetWithPagingFilterFull(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
		string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		GenericPagingModel<T2> model = new();
		try
		{
			IQueryable<T2> qModel = !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression) : context.Set<T>().Where(whereExpression).Select(selectExpression);
			model.TotalRecords = await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = await qModel.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
		Expression<Func<T, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		return !full ? GetWithPagingFilter(whereExpression, selectExpression, ascendingOrderExpression, skip, pageSize, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithPagingFilterFull(whereExpression, selectExpression, ascendingOrderExpression, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
		Expression<Func<T, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		GenericPagingModel<T2> model = new();
		try
		{
			IQueryable<T2> qModel = !trackEntities ? context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().Select(selectExpression) :
				context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression);

			var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() })
				.Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

			model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = results.ConvertAll(x => x.Entities);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0,
		int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		GenericPagingModel<T2> model = new();
		try
		{
			IQueryable<T2> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, false, trackEntities, fullQueryOptions);
			model.TotalRecords = await qModel.CountAsync(cancellationToken).ConfigureAwait(false); //results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = await qModel.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);//results.ConvertAll(x => x.Entities);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T2> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, true, fullQueryOptions: fullQueryOptions);
					var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

					model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
					model.Entities = results.ConvertAll(x => x.Entities);

					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
		Expression<Func<T, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where T2 : class
	{
		GenericPagingModel<T2> model = new();
		try
		{
			IQueryable<T2> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderExpression, queryTimeout, false, trackEntities, fullQueryOptions);

			var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

			model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = results.ConvertAll(x => x.Entities);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T2> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
					var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

					model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
					model.Entities = results.ConvertAll(x => x.Entities);

					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public IQueryable<T2> GetQueryPagingWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString,
		TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null) where T2 : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty),
				true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty),
				_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty)
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query.Select(selectExpression);
	}

	/// <summary>
	/// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
	public IQueryable<T2> GetQueryPagingWithFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderExpression,
		TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null) where T2 : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		IQueryable<T> query = !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
				true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
				_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression)
			};

		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
		}
		return query.Select(selectExpression);
	}

	#endregion

	#region GetOneWithFilter

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> with or without navigation properties that also satisfies the conditions of the linq query expression.</returns>
	public Task<T?> GetOneWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetOneWithFilter(whereExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetOneWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] LIMIT 1 or SELECT TOP 1 * WHERE [condition] LIMIT 1 query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of the linq query expression.</returns>
	public async Task<T?> GetOneWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				model = trackEntities ? await context.Set<T>().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, then transforms it into the <typeparamref name="T2"/> class with the select expression.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> with or without navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="T2"/> class.</returns>
	public Task<T2?> GetOneWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetOneWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetOneWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="T2"/> class with a select expression.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of the linq query expression that has been transformed into the <typeparamref name="T2"/> class with the select expression.</returns>
	public async Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T2? model = default;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				model = trackEntities ? await context.Set<T>().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<T>().AsNoTracking().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> with its navigation properties that also satisfies the conditions of the linq query expression.</returns>
	public async Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<UT>();
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions),
			};

			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
			}
			model = await query.FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
					{
						null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
						true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
						_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions),
					};

					if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
					{
						query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
					}
					model = await query.FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="T2"/> class with the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> with its navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="T2"/> class.</returns>
	public async Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T2? model = default;
		try
		{
			IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
			};

			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
			}
			model = await query.Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
					{
						null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
						true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
						_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
					};

					if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
					{
						query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
					}
					model = await query.Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	#endregion

	#region GetMaxByOrder

	/// <summary>
	/// Uses a descending order expression to return the record containing the maximum value according to that order with or without navigation properties.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression with or without navigation properties.</returns>
	public Task<T?> GetMaxByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMaxByOrder(whereExpression, descendingOrderExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetMaxByOrderFull(whereExpression, descendingOrderExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a descending order expression to return the record containing the maximum value according to that order.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression.</returns>
	public async Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters().Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters().Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				model = trackEntities ? await context.Set<T>().Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<T>().AsNoTracking().Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Uses a descending order expression to return the record and its navigation properties containing the maximum value according to that order.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression with it's navigation properties</returns>
	public async Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression),
			};

			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
			}
			model = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
					{
						null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression),
						true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression),
						_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression),
					};

					if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
					{
						query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
					}
					model = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	#endregion

	#region GetMinByOrder

	/// <summary>
	/// Uses a ascending order expression to return the record containing the minimum value according to that order with or without navigation properties.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="ascendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the minimum value according to the ascending order expression with or without navigation properties.</returns>
	public Task<T?> GetMinByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMinByOrder(whereExpression, ascendingOrderExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetMinByOrderFull(whereExpression, ascendingOrderExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a ascending order expression to return the record containing the minimum value according to that order.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="ascendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the minimum value according to the ascending order expression.</returns>
	public async Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters().Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters().Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				model = trackEntities ? await context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<T>().AsNoTracking().Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Uses a ascending order expression to return the record and its navigation properties containing the minimum value according to that order.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="ascendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the minimum value according to the ascending order expression with it's navigation properties.</returns>
	public async Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T? model = null;
		try
		{
			IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
			};

			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
			}
			model = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
					{
						null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
						true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
						_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression),
					};

					if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
					{
						query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
					}
					model = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	#endregion

	#region GetMax

	/// <summary>
	/// Uses a max expression to return the record containing the maximum object specified with or without navigation properties.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum object specified by the max expression with or without navigation properties.</returns>
	public Task<T2?> GetMax<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMax(whereExpression, maxExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetMaxFull(whereExpression, maxExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a max expression to return the record containing the maximum value specified.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum value specified by the max expression.</returns>
	public async Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T2? model = default;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				model = trackEntities ? await context.Set<T>().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<T>().AsNoTracking().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Uses a max expression to return the record containing the maximum object specified and its navigation properties.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum object specified by the min expression.</returns>
	public async Task<T2?> GetMaxFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T2? model = default;
		try
		{
			IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
			};

			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
			}
			model = await query.MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
					{
						null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
						true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
						_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
					};

					if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
					{
						query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
					}
					model = await query.MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	#endregion

	#region GetMin

	/// <summary>
	/// Uses a min expression to return the record containing the minimum object specified with or without navigation properties.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum object specified by the min expression with or without navigation properties</returns>
	public Task<T2?> GetMin<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMin(whereExpression, minExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken: cancellationToken) :
			GetMinFull(whereExpression, minExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a min expression to return the record containing the minimum value specified.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum value specified by the min expression.</returns>
	public async Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T2? model = default;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().AsNoTracking().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					model = trackEntities ? await context.Set<T>().IgnoreQueryFilters().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
						await context.Set<T>().IgnoreQueryFilters().AsNoTracking().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				model = trackEntities ? await context.Set<T>().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<T>().AsNoTracking().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	/// <summary>
	/// Uses a min expression to return the record containing the minimum object specified and its navigation properties.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum object specified by the min expression.</returns>
	public async Task<T2?> GetMinFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		T2? model = default;
		try
		{
			IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
			};

			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
			}
			model = await query.MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<T> query = fullQueryOptions.SplitQueryOverride switch
					{
						null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
						true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
						_ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
					};

					if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
					{
						query = (globalFilterOptions.FilterNamesToDisable?.Length > 0) ? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable) : query.IgnoreQueryFilters();
					}
					model = await query.MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
					logger.Warn(AddCircularRefTemplate, typeof(T).Name);
					circularReferencingEntities.TryAdd(typeof(T), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
					logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
				}
			}
			else
			{
				logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return model;
	}

	#endregion

	/// <summary>
	/// Gets the number of records in the table represented by T that satisfy the where expression.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records that satisfy the where expression.</returns>
	public async Task<int> GetCount(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		int count = 0;
		try
		{
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					count = await context.Set<T>().Where(whereExpression).AsNoTracking().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable).CountAsync(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					count = await context.Set<T>().Where(whereExpression).AsNoTracking().IgnoreQueryFilters().CountAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				count = await context.Set<T>().Where(whereExpression).AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return count;
	}

	#endregion Read

	#region Write

	/// <summary>
	/// Creates a new record in the table corresponding to type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="T"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public async Task Create(T model, bool removeNavigationProps = false)
	{
		if (model == null)
		{
			throw new ArgumentNullException(nameof(model), "Model cannot be null");
		}

		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (removeNavigationProps)
		{
			model.RemoveNavigationProperties(context);
		}

		try
		{
			await context.Set<T>().AddAsync(model).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Model}", ex.GetLocationOfException(), JsonSerializer.Serialize(model, defaultJsonSerializerOptions));
		}
	}

	/// <summary>
	/// Creates new records in the table corresponding to type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="model">Records of type <typeparamref name="T"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public async Task CreateMany(IEnumerable<T> model, bool removeNavigationProps = false)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (removeNavigationProps)
		{
			model.SetValue(x => x.RemoveNavigationProperties(context));
		}

		try
		{
			//await context.Set<T>().BulkInsertAsync(model); //Doesn't give updated identity values. EF Core Extensions (Paid)
			await context.Set<T>().AddRangeAsync(model).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Model}", ex.GetLocationOfException(), JsonSerializer.Serialize(model, defaultJsonSerializerOptions));
		}
	}

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="T"/> matching the object of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="T"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public void DeleteByObject(T model, bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;

		try
		{
			if (removeNavigationProps)
			{
				model.RemoveNavigationProperties(context);
			}

			DbSet<T> table;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
				}
			}
			else
			{
				table = context.Set<T>();
			}
			table.Remove(model);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Model}", ex.GetLocationOfException(), JsonSerializer.Serialize(model, defaultJsonSerializerOptions));
		}
	}

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="T"/> matching the primary key passed in.
	/// </summary>
	/// <param name="key">Key of the record of type <typeparamref name="T"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteByKey(object key, GlobalFilterOptions? globalFilterOptions = null)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		DbSet<T> table;
		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			if (globalFilterOptions.FilterNamesToDisable.AnyFast())
			{
				table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
			}
			else
			{
				table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
			}
		}
		else
		{
			table = context.Set<T>();
		}
		try
		{
			T? deleteItem = await table.FindAsync(key).ConfigureAwait(false);
			if (deleteItem != null)
			{
				table.Remove(deleteItem);
				return true;
			}
			//changes = await table.DeleteByKeyAsync(key); //EF Core +, Does not require save changes, Does not work with PostgreSQL
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tKey: {Key}", ex.GetLocationOfException(), JsonSerializer.Serialize(key));
		}
		return false;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="T"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public bool DeleteMany(IEnumerable<T> models, bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		try
		{
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context));
			}

			DbSet<T> table;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
				}
			}
			else
			{
				table = context.Set<T>();
			}

			table.RemoveRange(models); //Requires separate save
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Models}", ex.GetLocationOfException(), JsonSerializer.Serialize(models, defaultJsonSerializerOptions));
		}
		return false;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the where expression passed in.
	/// </summary>
	/// <param name="whereExpression">Expression to filter the records to delete.</param>
	/// <returns>The number of records deleted, or <see langword="null"/> if there was an error.</returns>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public async Task<int?> DeleteMany(Expression<Func<T, bool>> whereExpression, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		try
		{
			DbSet<T> table;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
				}
			}
			else
			{
				table = context.Set<T>();
			}

			return await table.AsNoTracking().Where(whereExpression).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tDelete Many Error", ex.GetLocationOfException());
		}
		return null;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="T"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteManyTracked(IEnumerable<T> models, bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		try
		{
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context));
			}

			DbSet<T> table;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
				}
			}
			else
			{
				table = context.Set<T>();
			}
			await table.DeleteRangeByKeyAsync(models).ConfigureAwait(false); //EF Core +, Does not require separate save
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Models}", ex.GetLocationOfException(), JsonSerializer.Serialize(models, defaultJsonSerializerOptions));
		}
		return false;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="keys">Keys of type <typeparamref name="T"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteManyByKeys(IEnumerable<object> keys, GlobalFilterOptions? globalFilterOptions = null) //Does not work with PostgreSQL, not testable
	{
		await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		try
		{
			DbSet<T> table;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
				}
			}
			else
			{
				table = context.Set<T>();
			}

			await table.DeleteRangeByKeyAsync(keys).ConfigureAwait(false); //EF Core +, Does not require separate save
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tKeys: {Keys}", ex.GetLocationOfException(), JsonSerializer.Serialize(keys));
		}
		return false;
	}

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="model">The modified entity.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public void Update(T model, bool removeNavigationProps = false) //Send in modified object
	{
		using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
		if (removeNavigationProps)
		{
			model.RemoveNavigationProperties(context);
		}
		context.Entry(model).State = EntityState.Modified;
	}

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="models">The modified entity.</param>>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public bool UpdateMany(List<T> models, bool removeNavigationProps = false, CancellationToken cancellationToken = default) //Send in modified objects
	{
		try
		{
			using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context), cancellationToken: cancellationToken);
			}
			//await context.BulkUpdateAsync(models); EF Core Extensions (Paid)
			context.UpdateRange(models);
			return true;
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{ErrorLocation} DBUpdate Error\n\tModels: {Models}", duex.GetLocationOfException(), JsonSerializer.Serialize(models));
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModels: {Models}", ex.GetLocationOfException(), JsonSerializer.Serialize(models));
		}
		return false;
	}

	/// <summary>
	/// Executes an update operation on records matching the where expression without loading them into memory.
	/// Uses EF Core's ExecuteUpdate for efficient bulk updates.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter records to update.</param>
	/// <param name="updateSetters"><see cref="UpdateSettersBuilder"/> defining the properties to update and how to update them.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records affected by the update operation, or <see langword="null"/> if there was an error.</returns>
	public async Task<int?> UpdateMany(Expression<Func<T, bool>> whereExpression, Action<UpdateSettersBuilder<T>> updateSetters,
		TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		try
		{
			await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
			if (queryTimeout != null)
			{
				context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
			}

			DbSet<T> table;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable);
				}
				else
				{
					table = (DbSet<T>)context.Set<T>().IgnoreQueryFilters();
				}
			}
			else
			{
				table = context.Set<T>();
			}
			return await table.AsNoTracking().Where(whereExpression).ExecuteUpdateAsync(updateSetters, cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{ErrorLocation} DBUpdate Error", duex.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return null;
	}

	/// <summary>
	/// Persist any tracked changes to the database.
	/// </summary>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> SaveChanges()
	{
		try
		{
			await using DbContext context = ServiceProvider.GetRequiredService<UT>()!;
			return await context.SaveChangesAsync().ConfigureAwait(false) > 0;
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{ErrorLocation} DBUpdate Error", duex.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return false;
	}

	#endregion Write
}

public sealed class GenericPagingModel<T>// where T : class
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
