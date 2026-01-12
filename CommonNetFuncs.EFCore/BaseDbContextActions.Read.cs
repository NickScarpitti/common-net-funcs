using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.EFCore;

public partial class BaseDbContextActions<TEntity, TContext> : IBaseDbContextActions<TEntity, TContext> where TEntity : class where TContext : DbContext
{
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
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public Task<TEntity?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
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
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);

		TEntity? model = null;
		try
		{
			// If we need to disable filters, we can't use FindAsync - need to build a query
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				// Get or create an expression builder for this entity type
				Func<object, Expression<Func<TEntity, bool>>> expressionBuilder = singleKeyExpressionBuilder.GetOrAdd(typeof(TEntity), type =>
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
						ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "x");
						MemberExpression property = Expression.Property(parameter, keyMetadata.KeyPropertyName!);
						ConstantExpression constant = Expression.Constant(keyValue, keyMetadata.KeyPropertyType!);
						BinaryExpression equality = Expression.Equal(property, constant);
						return Expression.Lambda<Func<TEntity, bool>>(equality, parameter);
					};
				});

				// Build the expression with the actual primary key value
				Expression<Func<TEntity, bool>> lambda = expressionBuilder(primaryKey);

				IQueryable<TEntity> query = context.Set<TEntity>().Where(lambda);

				// Apply filter disabling
				if (globalFilterOptions.FilterNamesToDisable.AnyFast())
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
				model = await context.Set<TEntity>().FindAsync([primaryKey], cancellationToken).ConfigureAwait(true);
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
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public Task<TEntity?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken) :
			GetByKeyFull(primaryKey, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Get individual record by a compound primary key.
	/// The values in the primaryKey array need to be ordered in the same order they are declared in AppDbContext
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="globalFilterOptions">Optional: Options for controlling global query filters. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);

		TEntity? model = null;
		try
		{
			// If we need to disable filters, we can't use FindAsync - need to build a query
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				// Get or create an expression builder for compound keys
				Func<object[], Expression<Func<TEntity, bool>>> expressionBuilder = compositeKeyExpressionBuilder.GetOrAdd(typeof(TEntity), type =>
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

						ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "x");
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

						return Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
					};
				});

				// Build the expression with the actual primary key values
				Expression<Func<TEntity, bool>> lambda = expressionBuilder(primaryKey);

				IQueryable<TEntity> query = context.Set<TEntity>().Where(lambda);

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
				model = await context.Set<TEntity>().FindAsync(primaryKey, cancellationToken).ConfigureAwait(false);
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
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		await using DbContext context = InitializeContext(queryTimeout);

		TEntity? model = null;
		try
		{
			model = await GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken).ConfigureAwait(false);
			if (model != null)
			{
				IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities);
				model = await query.GetObjectByPartialAsync(context, model, cancellationToken: cancellationToken);
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
						IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, true, false);
						model = await query.GetObjectByPartialAsync(context, model, cancellationToken: cancellationToken);
					}
					logger.Warn(AddCircularRefTemplate, typeof(TEntity).Name);
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
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
	/// Get individual record by the primary key with all navigation properties.
	/// If using a compound primary key, use an object of the same <see langword="class"/> to be returned with the primary key fields populated.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);

		TEntity? model = null;
		try
		{
			model = await GetByKey(primaryKey, queryTimeout, globalFilterOptions, cancellationToken).ConfigureAwait(false);
			if (model != null)
			{
				IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities);
				model = await query.GetObjectByPartialAsync(context, model, cancellationToken: cancellationToken);
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
						IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, true, false);
						model = await query.GetObjectByPartialAsync(context, model, cancellationToken: cancellationToken);
					}
					logger.Warn(AddCircularRefTemplate, typeof(TEntity).Name);
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public Task<List<TEntity>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public async Task<List<TEntity>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return await ExecuteQueryWithErrorLogging(async () =>
			await GetQueryAll(queryTimeout, trackEntities, globalFilterOptions).ToListAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public Task<List<TOutput>?> GetAll<TOutput>(bool full, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAll(selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetAllFull(selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table and transforms them into the type <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public async Task<List<TOutput>?> GetAll<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return await ExecuteQueryWithErrorLogging(async () =>
			await GetQueryAll(selectExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IAsyncEnumerable<TEntity>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAllStreaming(queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetAllFullStreaming(queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IAsyncEnumerable<TEntity>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return ExecuteStreaming(() => GetQueryAll(queryTimeout, trackEntities), cancellationToken);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IAsyncEnumerable<TOutput>? GetAllStreaming<TOutput>(bool full, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAllStreaming(selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetAllFullStreaming(selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public IAsyncEnumerable<TOutput>? GetAllStreaming<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return ExecuteStreaming(() => GetQueryAll(selectExpression, queryTimeout, trackEntities), cancellationToken);
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IQueryable<TEntity> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = InitializeContext(queryTimeout);
		return ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions);
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryAll<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = InitializeContext(queryTimeout);
		return ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Select(selectExpression);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public async Task<List<TEntity>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		return await ExecuteWithCircularRefHandling(async (handlingCircularRef, cancellationToken) =>
			await GetQueryAllFull(queryTimeout, handlingCircularRef, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public async Task<List<TOutput>?> GetAllFull<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions, globalFilterOptions).ToListAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IAsyncEnumerable<TEntity>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		return ExecuteStreamingWithCircularRefHandling((_) => GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions), cancellationToken);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public IAsyncEnumerable<TOutput>? GetAllFullStreaming<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		return ExecuteStreamingWithCircularRefHandling((_) => GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions), cancellationToken);
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IQueryable<TEntity> GetQueryAllFull(TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, handlingCircularRefException, trackEntities);
		return ApplyGlobalFilters(query, globalFilterOptions);
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryAllFull<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, handlingCircularRefException, trackEntities);
		return ApplyGlobalFilters(query, globalFilterOptions).Select(selectExpression);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public Task<List<TEntity>?> GetWithFilter(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilter(whereExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<TEntity>?> GetWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return await ExecuteQueryWithErrorLogging(async () =>
			await GetQueryWithFilter(whereExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public Task<List<TOutput>?> GetWithFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<TOutput>?> GetWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return await ExecuteQueryWithErrorLogging(async () =>
			await GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IAsyncEnumerable<TEntity>? GetWithFilterStreaming(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilterStreaming(whereExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithFilterFullStreaming(whereExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IAsyncEnumerable<TEntity>? GetWithFilterStreaming(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return ExecuteStreaming(() => GetQueryWithFilter(whereExpression, queryTimeout, trackEntities), cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public IAsyncEnumerable<TOutput>? GetWithFilterStreaming<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IAsyncEnumerable<TOutput>? GetWithFilterStreaming<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return ExecuteStreaming(() => GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities), cancellationToken);
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<TEntity> GetQueryWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = InitializeContext(queryTimeout);
		return ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression);
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<TOutput> GetQueryWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = InitializeContext(queryTimeout);
		return ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).Select(selectExpression).Distinct();
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<TEntity>?> GetWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public async Task<List<TOutput>?> GetWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IAsyncEnumerable<TEntity>? GetWithFilterFullStreaming(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		return ExecuteStreamingWithCircularRefHandling((_) => GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions), cancellationToken);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public IAsyncEnumerable<TOutput>? GetWithFilterFullStreaming<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		return ExecuteStreamingWithCircularRefHandling((_) => GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions), cancellationToken);
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
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<TEntity> GetQueryWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, handlingCircularRefException, trackEntities).Where(whereExpression);
		return ApplyGlobalFilters(query, globalFilterOptions);
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public IQueryable<TOutput> GetQueryWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, handlingCircularRefException, trackEntities);
		return ApplyGlobalFilters(query, globalFilterOptions).Where(whereExpression).Select(selectExpression).Distinct();
	}

	#endregion

	#region GetNavigationWithFilter

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> with or without its navigation properties using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> class with the select expression.</returns>
	public Task<List<TEntity>?> GetNavigationWithFilter<TOutput>(bool full, Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetNavigationWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions: globalFilterOptions, cancellationToken: cancellationToken) :
			GetNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public async Task<List<TEntity>?> GetNavigationWithFilter<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> with or without its navigation properties using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public IAsyncEnumerable<TEntity>? GetNavigationWithFilterStreaming<TOutput>(bool full, Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetNavigationWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions: globalFilterOptions, cancellationToken: cancellationToken) :
			GetNavigationWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public IAsyncEnumerable<TEntity>? GetNavigationWithFilterStreaming<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new();
		return ExecuteStreamingWithCircularRefHandling((_) => GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions), cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public async Task<List<TEntity>?> GetNavigationWithFilterFull<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		IQueryable<TEntity> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await BuildFullQuery(query, context, fullQueryOptions, false, trackEntities).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public IAsyncEnumerable<TEntity>? GetNavigationWithFilterFullStreaming<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new();
		IQueryable<TEntity> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;

		return ExecuteStreamingWithCircularRefHandling((_) => BuildFullQuery(query, context, fullQueryOptions, false, trackEntities).Distinct(), cancellationToken);
	}

	/// <summary>
	/// Gets query to get the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public IQueryable<TEntity> GetQueryNavigationWithFilterFull<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery<TOutput>(context, fullQueryOptions, handlingCircularRefException, trackEntities).Where(whereExpression).Select(selectExpression).Distinct();
		return ApplyGlobalFilters(query, globalFilterOptions);
	}

	#endregion

	#region GetWithPagingFilter

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null,
		int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetWithPagingFilter(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithPagingFilterFull(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null,
		CancellationToken cancellationToken = default) where TOutput : class
	{
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TOutput> qModel = ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).Select(selectExpression);
		return await ExecuteQueryWithErrorLogging(async () => await BuildPagingResult(qModel, skip, pageSize, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false) ?? new();
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
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
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput, TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetWithPagingFilter(whereExpression, selectExpression, ascendingOrderExpression, skip, pageSize, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetWithPagingFilterFull(whereExpression, selectExpression, ascendingOrderExpression, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TOutput> qModel = ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression);
		return await ExecuteQueryWithErrorLogging(async () => await BuildPagingResult(qModel, skip, pageSize, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false) ?? new();
	}

	/// <summary>
	/// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null, int skip = 0,
		int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		IQueryable<TOutput> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, false, trackEntities, fullQueryOptions);
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		return (await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await BuildPagingResult(qModel, skip, pageSize, cancellationToken).ConfigureAwait(false), cancellationToken))!;
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
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
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilterFull<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		IQueryable<TOutput> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderExpression, queryTimeout, false, trackEntities, fullQueryOptions);
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		return (await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await BuildPagingResult(qModel, skip, pageSize, cancellationToken).ConfigureAwait(false), cancellationToken))!;
	}

	/// <summary>
	/// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryPagingWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString,
		TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, handlingCircularRefException, trackEntities).Where(whereExpression).OrderBy(orderByString ?? string.Empty);
		return ApplyGlobalFilters(query, globalFilterOptions).Select(selectExpression);
	}

	/// <summary>
	/// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryPagingWithFilterFull<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression,
		TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, handlingCircularRefException, trackEntities).Where(whereExpression).OrderBy(ascendingOrderExpression);
		return ApplyGlobalFilters(query, globalFilterOptions).Select(selectExpression);
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
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with or without navigation properties that also satisfies the conditions of the linq query expression.</returns>
	public Task<TEntity?> GetOneWithFilter(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetOneWithFilter(whereExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetOneWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] LIMIT 1 or SELECT TOP 1 * WHERE [condition] LIMIT 1 query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of the linq query expression.</returns>
	public async Task<TEntity?> GetOneWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, then transforms it into the <typeparamref name="TOutput"/> class with the select expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with or without navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="TOutput"/> class.</returns>
	public Task<TOutput?> GetOneWithFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetOneWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetOneWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="TOutput"/> class with a select expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of the linq query expression that has been transformed into the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public async Task<TOutput?> GetOneWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
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
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with its navigation properties that also satisfies the conditions of the linq query expression.</returns>
	public async Task<TEntity?> GetOneWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities).Where(whereExpression);
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await ApplyGlobalFilters(query, globalFilterOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="TOutput"/> class with the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with its navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="TOutput"/> class.</returns>
	public async Task<TOutput?> GetOneWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities).Where(whereExpression);
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await ApplyGlobalFilters(query, globalFilterOptions).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
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
	public Task<TEntity?> GetMaxByOrder<TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMaxByOrder(whereExpression, descendingOrderExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
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
	public async Task<TEntity?> GetMaxByOrder<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
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
	public async Task<TEntity?> GetMaxByOrderFull<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities).Where(whereExpression).OrderByDescending(descendingOrderExpression);
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await ApplyGlobalFilters(query, globalFilterOptions).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
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
	public Task<TEntity?> GetMinByOrder<TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMinByOrder(whereExpression, ascendingOrderExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
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
	public async Task<TEntity?> GetMinByOrder<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
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
	public async Task<TEntity?> GetMinByOrderFull<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities).Where(whereExpression).OrderBy(ascendingOrderExpression);
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await ApplyGlobalFilters(query, globalFilterOptions).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	#endregion

	#region GetMax

	/// <summary>
	/// Uses a max expression to return the record containing the maximum object specified with or without navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum object specified by the max expression with or without navigation properties.</returns>
	public Task<TOutput?> GetMax<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMax(whereExpression, maxExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetMaxFull(whereExpression, maxExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a max expression to return the record containing the maximum value specified.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum value specified by the max expression.</returns>
	public async Task<TOutput?> GetMax<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Uses a max expression to return the record containing the maximum object specified and its navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum object specified by the min expression.</returns>
	public async Task<TOutput?> GetMaxFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities).Where(whereExpression);
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await ApplyGlobalFilters(query, globalFilterOptions).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	#endregion

	#region GetMin

	/// <summary>
	/// Uses a min expression to return the record containing the minimum object specified with or without navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum object specified by the min expression with or without navigation properties</returns>
	public Task<TOutput?> GetMin<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMin(whereExpression, minExpression, queryTimeout, trackEntities, globalFilterOptions, cancellationToken) :
			GetMinFull(whereExpression, minExpression, queryTimeout, trackEntities, fullQueryOptions, globalFilterOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a min expression to return the record containing the minimum value specified.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum value specified by the min expression.</returns>
	public async Task<TOutput?> GetMin<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await ApplyTrackingAndFilters(context, trackEntities, globalFilterOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Uses a min expression to return the record containing the minimum object specified and its navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum object specified by the min expression.</returns>
	public async Task<TOutput?> GetMinFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = InitializeContext(queryTimeout);
		IQueryable<TEntity> query = BuildFullQuery(context, fullQueryOptions, false, trackEntities).Where(whereExpression);
		return await ExecuteWithCircularRefHandling(async (_, cancellationToken) =>
			await ApplyGlobalFilters(query, globalFilterOptions).MinAsync(minExpression, cancellationToken).ConfigureAwait(false), cancellationToken);
	}

	#endregion

	/// <summary>
	/// Gets the number of records in the table represented by TObj that satisfy the where expression.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records that satisfy the where expression.</returns>
	public async Task<int> GetCount(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = InitializeContext(queryTimeout);
		return await ExecuteQueryWithErrorLogging(async () =>
			await GetDbSetWithFilters(context, globalFilterOptions).Where(whereExpression).CountAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
	}

	#endregion Read
}
