using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.Json;
using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.EFCore;

public partial class BaseDbContextActions<TEntity, TContext> : IBaseDbContextActions<TEntity, TContext> where TEntity : class where TContext : DbContext
{
	#region Write

	/// <summary>
	/// Creates a new record in the table corresponding to type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="TEntity"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public async Task Create(TEntity model, bool removeNavigationProps = false)
	{
		if (model == null)
		{
			throw new ArgumentNullException(nameof(model), "Model cannot be null");
		}

		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (removeNavigationProps)
		{
			model.RemoveNavigationProperties(context);
		}

		try
		{
			await context.Set<TEntity>().AddAsync(model).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Model}", ex.GetLocationOfException(), JsonSerializer.Serialize(model, defaultJsonSerializerOptions));
		}
	}

	/// <summary>
	/// Creates new records in the table corresponding to type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="model">Records of type <typeparamref name="TEntity"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public async Task CreateMany(IEnumerable<TEntity> model, bool removeNavigationProps = false)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (removeNavigationProps)
		{
			model.SetValue(x => x.RemoveNavigationProperties(context));
		}

		try
		{
			//await context.Set<TObj>().BulkInsertAsync(model); //Doesn't give updated identity values. EF Core Extensions (Paid)
			await context.Set<TEntity>().AddRangeAsync(model).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Model}", ex.GetLocationOfException(), JsonSerializer.Serialize(model, defaultJsonSerializerOptions));
		}
	}

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="TEntity"/> matching the object of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <param name="globalFilterOptions">Optional: Global filter options (not applicable to this operation as it works with loaded entities).</param>
	public void DeleteByObject(TEntity model, bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;

		try
		{
			if (removeNavigationProps)
			{
				model.RemoveNavigationProperties(context);
			}

			// Note: Global filters don't apply to Remove operations on already-loaded entities
			DbSet<TEntity> table = context.Set<TEntity>();
			table.Remove(model);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tModel: {Model}", ex.GetLocationOfException(), JsonSerializer.Serialize(model, defaultJsonSerializerOptions));
		}
	}

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="TEntity"/> matching the primary key passed in.
	/// </summary>
	/// <param name="key">Key of the record of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="globalFilterOptions">Optional: Options for controlling global query filters.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteByKey(object key, GlobalFilterOptions? globalFilterOptions = null)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		DbSet<TEntity> table = context.Set<TEntity>();
		try
		{
			TEntity? deleteItem = null;
			if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
			{
				// Need to apply global filter options to the query in order to find the entity to delete, since Find/FindAsync does not allow for ignoring filters
				deleteItem = await GetByKey(false, key, globalFilterOptions: globalFilterOptions).ConfigureAwait(false);
			}
			else
			{
				deleteItem = await table.FindAsync(key).ConfigureAwait(false);
			}

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
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <param name="globalFilterOptions">Optional: Global filter options (not applicable to this operation as it works with loaded entities).</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public bool DeleteMany(IEnumerable<TEntity> models, bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context));
			}

			// Note: Global filters don't apply to RemoveRange operations on already-loaded entities
			DbSet<TEntity> table = context.Set<TEntity>();
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
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the where expression passed in.
	/// </summary>
	/// <param name="whereExpression">Expression to filter the records to delete.</param>
	/// <returns>The number of records deleted, or <see langword="null"/> if there was an error.</returns>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public async Task<int?> DeleteMany(Expression<Func<TEntity, bool>> whereExpression, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			DbSet<TEntity> table = context.Set<TEntity>();
			IQueryable<TEntity> query = ApplyGlobalFilters(table, globalFilterOptions);
			return await query.AsNoTracking().Where(whereExpression).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error\n\tDelete Many Error", ex.GetLocationOfException());
		}
		return null;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <param name="globalFilterOptions">Optional: Global filter options (not applicable to this operation as it works with loaded entities).</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteManyTracked(IEnumerable<TEntity> models, bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context));
			}

			// Note: Global filters don't apply to DeleteRangeByKeyAsync operations
			DbSet<TEntity> table = context.Set<TEntity>();
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
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="keys">Keys of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="globalFilterOptions">Optional: Global filter options (not applicable to this operation).</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteManyByKeys(IEnumerable<object> keys, GlobalFilterOptions? globalFilterOptions = null) //Does not work with PostgreSQL, not testable
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			// Note: Global filters don't apply to DeleteRangeByKeyAsync operations
			DbSet<TEntity> table = context.Set<TEntity>();
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
	public void Update(TEntity model, bool removeNavigationProps = false) //Send in modified object
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
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
	public bool UpdateMany(List<TEntity> models, bool removeNavigationProps = false, CancellationToken cancellationToken = default) //Send in modified objects
	{
		try
		{
			using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
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
	public async Task<int?> UpdateMany(Expression<Func<TEntity, bool>> whereExpression, Action<UpdateSettersBuilder<TEntity>> updateSetters,
		TimeSpan? queryTimeout = null, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default)
	{
		try
		{
			await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
			if (queryTimeout != null)
			{
				context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
			}

			DbSet<TEntity> table = context.Set<TEntity>();
			IQueryable<TEntity> query = ApplyGlobalFilters(table, globalFilterOptions);
			return await query.AsNoTracking().Where(whereExpression).ExecuteUpdateAsync(updateSetters, cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{ErrorLocation} DBUpdate Error", duex.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
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
			await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
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
