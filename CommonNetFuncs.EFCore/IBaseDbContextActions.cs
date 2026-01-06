using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CommonNetFuncs.EFCore;

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="TEntity">Entity <see langword="class"/> to be used with these methods.</typeparam>
/// <typeparam name="TContext">DB Context for the database you with to run these actions against.</typeparam>
#pragma warning disable S2326 // Unused type parameters should be removed
public interface IBaseDbContextActions<TEntity, TContext> where TEntity : class? where TContext : DbContext
#pragma warning restore S2326 // Unused type parameters should be removed
{
	#region Read

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
	Task<TEntity?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get individual record by the single field primary key.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	Task<TEntity?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get individual record by a compound primary key.
	/// The values in the primaryKey array need to be ordered in the same order they are declared in AppDbContext
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	Task<TEntity?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<TEntity>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	Task<List<TEntity>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	Task<List<TOutput>?> GetAll<TOutput>(bool full, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<TOutput>?> GetAll<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<TEntity>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	IAsyncEnumerable<TEntity>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	IAsyncEnumerable<TOutput>? GetAllStreaming<TOutput>(bool full, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<TOutput>? GetAllStreaming<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets query to get all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	IQueryable<TEntity> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets query to get all records from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	IQueryable<TOutput> GetQueryAll<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	Task<List<TEntity>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<TOutput>?> GetAllFull<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	IAsyncEnumerable<TEntity>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<TOutput>? GetAllFullStreaming<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	IQueryable<TEntity> GetQueryAllFull(TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null);

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
	/// <returns>IQueryable for records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	IQueryable<TOutput> GetQueryAllFull<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter, with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	Task<List<TEntity>?> GetWithFilter(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	Task<List<TEntity>?> GetWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	Task<List<TOutput>?> GetWithFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	Task<List<TOutput>?> GetWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter, with or without navigation properties, as an async stream.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Async stream of records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	IAsyncEnumerable<TEntity>? GetWithFilterStreaming(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter as an async stream.
	/// </summary>
	/// <param name="expression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Async stream of records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	IAsyncEnumerable<TEntity>? GetWithFilterStreaming(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<TOutput>? GetWithFilterStreaming<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	IAsyncEnumerable<TOutput>? GetWithFilterStreaming<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a queryable for records from the corresponding table that match the specified filter.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>IQueryable for records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	IQueryable<TEntity> GetQueryWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	IQueryable<TOutput> GetQueryWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter, including navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	Task<List<TEntity>?> GetWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<TOutput>?> GetWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter, including navigation properties, as an async stream.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="expression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Async stream of records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	IAsyncEnumerable<TEntity>? GetWithFilterFullStreaming(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<TOutput>? GetWithFilterFullStreaming<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a queryable for records from the corresponding table that match the specified filter, including navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>IQueryable for records from the table corresponding to class <typeparamref name="TEntity"/> that match the filter.</returns>
	IQueryable<TEntity> GetQueryWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null);

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
	IQueryable<TOutput> GetQueryWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
				bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null);

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
	Task<List<TEntity>?> GetNavigationWithFilter<TOutput>(bool full, Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<List<TEntity>?> GetNavigationWithFilter<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	IAsyncEnumerable<TEntity>? GetNavigationWithFilterStreaming<TOutput>(bool full, Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	IAsyncEnumerable<TEntity>? GetNavigationWithFilterStreaming<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<List<TEntity>?> GetNavigationWithFilterFull<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	IAsyncEnumerable<TEntity>? GetNavigationWithFilterFullStreaming<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	IQueryable<TEntity> GetQueryNavigationWithFilterFull<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
				bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where TOutput : class;

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
	Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null, int skip = 0,
				int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0,
				TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput, TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression,
				int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0,
				int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<GenericPagingModel<TOutput>> GetWithPagingFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0,
				TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
				CancellationToken cancellationToken = default) where TOutput : class;

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
	Task<GenericPagingModel<TOutput>> GetWithPagingFilterFull<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression,
				int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class;

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
	IQueryable<TOutput> GetQueryPagingWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString, TimeSpan? queryTimeout = null,
				bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where TOutput : class;

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
	IQueryable<TOutput> GetQueryPagingWithFilterFull<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression,
				TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where TOutput : class;

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
	Task<TEntity?> GetOneWithFilter(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] LIMIT 1 or SELECT TOP 1 * WHERE [condition] LIMIT 1 query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of the linq query expression.</returns>
	Task<TEntity?> GetOneWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetOneWithFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetOneWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetOneWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetOneWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetMaxByOrder<TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Uses a descending order expression to return the record containing the maximum value according to that order.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression.</returns>
	Task<TEntity?> GetMaxByOrder<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetMaxByOrderFull<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetMinByOrder<TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetMinByOrder<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<TEntity?> GetMinByOrderFull<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetMax<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetMax<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetMaxFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetMin<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetMin<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<TOutput?> GetMinFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the number of records in the table represented by T that satisfy the where expression.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records that satisfy the where expression.</returns>
	Task<int> GetCount(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

	#endregion Read

	#region Write

	/// <summary>
	/// Creates a new record in the table corresponding to type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="TEntity"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	Task Create(TEntity model, bool removeNavigationProps = false);

	/// <summary>
	/// Creates new records in the table corresponding to type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="model">Records of type <typeparamref name="TEntity"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	Task CreateMany(IEnumerable<TEntity> model, bool removeNavigationProps = false);

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="TEntity"/> matching the object of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	void DeleteByObject(TEntity model, bool removeNavigationProps = false);

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="TEntity"/> matching the primary key passed in.
	/// </summary>
	/// <param name="key">Key of the record of type <typeparamref name="TEntity"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	Task<bool> DeleteByKey(object key);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	bool DeleteMany(IEnumerable<TEntity> models, bool removeNavigationProps = false);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the where expression passed in.
	/// </summary>
	/// <param name="whereExpression">The LINQ expression to filter the records to delete.</param>
	/// <returns>The number of records deleted, or <see langword="null"/> if there was an error.</returns>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	Task<int?> DeleteMany(Expression<Func<TEntity, bool>> whereExpression, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	Task<bool> DeleteManyTracked(IEnumerable<TEntity> models, bool removeNavigationProps = false);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="keys">Keys of type <typeparamref name="TEntity"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	Task<bool> DeleteManyByKeys(IEnumerable<object> keys);

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="model">The modified entity.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	void Update(TEntity model, bool removeNavigationProps = false);

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="models">The modified entity.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns>The number of records affected by the update operation, or <see langword="null"/> if there was an error.</returns>
	bool UpdateMany(List<TEntity> models, bool removeNavigationProps = false, CancellationToken cancellationToken = default);

	Task<int?> UpdateMany(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
	 TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Persist any tracked changes to the database.
	/// </summary>
	/// <returns>Boolean indicating success.</returns>
	Task<bool> SaveChanges();

	#endregion Write
}
