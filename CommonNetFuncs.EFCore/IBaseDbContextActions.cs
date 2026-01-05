using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CommonNetFuncs.EFCore;

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="T">Entity <see langword="class"/> to be used with these methods.</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against.</typeparam>
#pragma warning disable S2326 // Unused type parameters should be removed
public interface IBaseDbContextActions<T, UT> where T : class? where UT : DbContext
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
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	Task<T?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get individual record by the single field primary key.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	Task<T?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

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
	Task<T?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get individual record by a compound primary key.
	/// The values in the primaryKey array need to be ordered in the same order they are declared in AppDbContext
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
	Task<T?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

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
	Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<T>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	Task<List<T>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	Task<List<T2>?> GetAll<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<T>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	IAsyncEnumerable<T>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	IAsyncEnumerable<T2>? GetAllStreaming<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<T2>? GetAllStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets query to get all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	IQueryable<T> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets query to get all records from the corresponding table and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
	IQueryable<T2> GetQueryAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	IAsyncEnumerable<T>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<T2>? GetAllFullStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
	IQueryable<T> GetQueryAllFull(TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null);

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
	/// <returns>IQueryable for records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	IQueryable<T2> GetQueryAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
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
	/// <returns>Records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	Task<List<T>?> GetWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	Task<List<T2>?> GetWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	/// <returns>Async stream of records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	IAsyncEnumerable<T>? GetWithFilterStreaming(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter as an async stream.
	/// </summary>
	/// <param name="expression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Async stream of records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	IAsyncEnumerable<T>? GetWithFilterStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a queryable for records from the corresponding table that match the specified filter.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>IQueryable for records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	IQueryable<T> GetQueryWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	IQueryable<T2> GetQueryWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);

	/// <summary>
	/// Gets records from the corresponding table that match the specified filter, including navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">Linq expression to filter the returned records.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
	/// <returns>Async stream of records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	IAsyncEnumerable<T>? GetWithFilterFullStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	IAsyncEnumerable<T2>? GetWithFilterFullStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
	/// <returns>IQueryable for records from the table corresponding to class <typeparamref name="T"/> that match the filter.</returns>
	IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
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
	/// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
	IQueryable<T2> GetQueryWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
				bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null);

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
	Task<List<T>?> GetNavigationWithFilter<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	Task<List<T>?> GetNavigationWithFilter<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	Task<List<T>?> GetNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	IAsyncEnumerable<T>? GetNavigationWithFilterFullStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	IQueryable<T> GetQueryNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
				bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where T2 : class;

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
	Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0,
				int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0,
				TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where T2 : class;

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
	Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderExpression,
				int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderExpression, int skip = 0,
				int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where T2 : class;

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
	Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0,
				TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
				CancellationToken cancellationToken = default) where T2 : class;

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
	Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderExpression,
				int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class;

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
	IQueryable<T2> GetQueryPagingWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString, TimeSpan? queryTimeout = null,
				bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where T2 : class;

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
	IQueryable<T2> GetQueryPagingWithFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderExpression,
				TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where T2 : class;

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
	Task<T?> GetOneWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] LIMIT 1 or SELECT TOP 1 * WHERE [condition] LIMIT 1 query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of the linq query expression.</returns>
	Task<T?> GetOneWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<T2?> GetOneWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
	Task<T?> GetMaxByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
	Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
	Task<T?> GetMinByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
	Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T2?> GetMax<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<T2?> GetMaxFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T2?> GetMin<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

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
	Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default);

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
	Task<T2?> GetMinFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
				FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the number of records in the table represented by T that satisfy the where expression.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records that satisfy the where expression.</returns>
	Task<int> GetCount(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

	#endregion Read

	#region Write

	/// <summary>
	/// Creates a new record in the table corresponding to type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="T"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	Task Create(T model, bool removeNavigationProps = false);

	/// <summary>
	/// Creates new records in the table corresponding to type <typeparamref name="T"/>.
	/// </summary>
	/// <param name="model">Records of type <typeparamref name="T"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	Task CreateMany(IEnumerable<T> model, bool removeNavigationProps = false);

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="T"/> matching the object of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="T"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	void DeleteByObject(T model, bool removeNavigationProps = false);

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="T"/> matching the primary key passed in.
	/// </summary>
	/// <param name="key">Key of the record of type <typeparamref name="T"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	Task<bool> DeleteByKey(object key);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="T"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	bool DeleteMany(IEnumerable<T> models, bool removeNavigationProps = false);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the where expression passed in.
	/// </summary>
	/// <param name="whereExpression">The LINQ expression to filter the records to delete.</param>
	/// <returns>The number of records deleted, or <see langword="null"/> if there was an error.</returns>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	Task<int?> DeleteMany(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="T"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	Task<bool> DeleteManyTracked(IEnumerable<T> models, bool removeNavigationProps = false);

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
	/// </summary>
	/// <param name="keys">Keys of type <typeparamref name="T"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	Task<bool> DeleteManyByKeys(IEnumerable<object> keys);

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="model">The modified entity.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	void Update(T model, bool removeNavigationProps = false);

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="models">The modified entity.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns>The number of records affected by the update operation, or <see langword="null"/> if there was an error.</returns>
	bool UpdateMany(List<T> models, bool removeNavigationProps = false, CancellationToken cancellationToken = default);

	Task<int?> UpdateMany(Expression<Func<T, bool>> whereExpression, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls,
	 TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Persist any tracked changes to the database.
	/// </summary>
	/// <returns>Boolean indicating success.</returns>
	Task<bool> SaveChanges();

	#endregion Write
}
