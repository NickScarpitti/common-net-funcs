using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Net;
using CommonNetFuncs.Core;
using CommonNetFuncs.EFCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using static CommonNetFuncs.Core.Copy;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.DeepClone.ExpressionTrees;

namespace CommonNetFuncs.Web.Api;

public sealed class GenericEndpoints : ControllerBase
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Basic endpoint to create more than one entity at a time
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being created</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="models">Entities to create</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<List<TEntity>>> CreateMany<TEntity, TContext>(IEnumerable<TEntity> models, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions, bool removeNavigationProps = false) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			await baseAppDbContextActions.CreateMany(models, removeNavigationProps).ConfigureAwait(false);
			if (await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
			{
				return Ok(models);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return NoContent();
	}

	/// <summary>
	/// Basic endpoint to delete one entity
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being deleted</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="model">Entity to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<TEntity>> Delete<TEntity, TContext>(TEntity model, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions, bool removeNavigationProps = false,
		GlobalFilterOptions? globalFilterOptions = null) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			baseAppDbContextActions.DeleteByObject(model, removeNavigationProps, globalFilterOptions);
			if (await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
			{
				return Ok(model);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return NoContent();
	}

	/// <summary>
	/// Basic endpoint to delete more than one entity at a time
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being deleted</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="models">Entities to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<List<TEntity>>> DeleteMany<TEntity, TContext>(IEnumerable<TEntity> models, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions,
		bool removeNavigationProps = false, GlobalFilterOptions? globalFilterOptions = null) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			if (models.Any() && baseAppDbContextActions.DeleteMany(models, removeNavigationProps, globalFilterOptions) && await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
			{
				return Ok(models);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return NoContent();
	}

	/// <summary>
	/// Basic endpoint to delete more than one entity at a time
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being deleted</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="whereClause">Where clause to filter entities to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<int>> DeleteMany<TEntity, TContext>(Expression<Func<TEntity, bool>> whereClause, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions,
		GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			int? result = await baseAppDbContextActions.DeleteMany(whereClause, globalFilterOptions, cancellationToken).ConfigureAwait(false);
			if (result != null)
			{
				return Ok(result);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return NoContent();
	}

	/// <summary>
	/// Basic endpoint to delete more than one entity at a time
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being deleted</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="models">Entities to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<List<TEntity>>> DeleteManyByKeys<TEntity, TContext>(IEnumerable<object> models, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions,
		GlobalFilterOptions? globalFilterOptions = null) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			if (models.Any() && await baseAppDbContextActions.DeleteManyByKeys(models, globalFilterOptions).ConfigureAwait(false)) //Does not work with PostgreSQL
			{
				return Ok(models);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return NoContent();
	}

	/// <summary>
	/// Basic endpoint to delete more than one entity at a time
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being deleted</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="whereClause">Where clause to filter entities to delete.</param>
	/// <param name="setPropertyCalls">Set property calls defining the updates to be made.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<int>> UpdateMany<TEntity, TContext>(Expression<Func<TEntity, bool>> whereClause, Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
			IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions, GlobalFilterOptions? globalFilterOptions = null, CancellationToken cancellationToken = default) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			int? result = await baseAppDbContextActions.UpdateMany(whereClause, setPropertyCalls, null, globalFilterOptions, cancellationToken).ConfigureAwait(false);
			if (result != null)
			{
				return Ok(result);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return NoContent();
	}

	/// <summary>
	/// Basic endpoint to update an entity with a single field primary key
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being updated</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="primaryKey">Primary key of the entity to update</param>
	/// <param name="patch">Patch document containing the updates to be made to the entity.</param>>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<TEntity>> Patch<TEntity, TContext>(object primaryKey, JsonPatchDocument<TEntity> patch, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions,
		GlobalFilterOptions? globalFilterOptions = null) where TEntity : class?, new() where TContext : DbContext
	{
		TEntity? dbModel = await baseAppDbContextActions.GetByKey(primaryKey, globalFilterOptions: globalFilterOptions).ConfigureAwait(false);
		return await PatchInternal(dbModel, patch, baseAppDbContextActions).ConfigureAwait(false);
	}

	/// <summary>
	/// Basic endpoint to update an entity with a multi-field primary key
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being updated</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="primaryKey">Ordered values comprising the key of the entity to update</param>
	/// <param name="patch">Patch document containing the updates to be made to the entity.</param>>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<TEntity>> Patch<TEntity, TContext>(object[] primaryKey, JsonPatchDocument<TEntity> patch, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions,
		GlobalFilterOptions? globalFilterOptions = null) where TEntity : class?, new() where TContext : DbContext
	{
		TEntity? dbModel = await baseAppDbContextActions.GetByKey(primaryKey, globalFilterOptions: globalFilterOptions).ConfigureAwait(false);
		return await PatchInternal(dbModel, patch, baseAppDbContextActions).ConfigureAwait(false);
	}

	/// <summary>
	/// Helper method to update an entity
	/// </summary>
	/// <typeparam name="TEntity">Type of entity being updated</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="dbModel">Entity to update</param>
	/// <param name="patch">Patch document containing the updates to be made to the entity.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	private async Task<ActionResult<TEntity>> PatchInternal<TEntity, TContext>(TEntity? dbModel, JsonPatchDocument<TEntity> patch, IBaseDbContextActions<TEntity, TContext> baseAppDbContextActions) where TEntity : class?, new() where TContext : DbContext
	{
		try
		{
			if (dbModel == null)
			{
				return NoContent();
			}

			if (patch.Operations.Count == 0)
			{
				return Ok(dbModel);
			}

			TEntity updateModel = dbModel.DeepClone();

			patch.ApplyTo(updateModel);

			List<ValidationResult> failedValidations = [];
			Validator.TryValidateObject(updateModel, new(updateModel), failedValidations);
			if (failedValidations.AnyFast())
			//if (!TryValidateModel(updateModel)) //Only works when this method is the controller endpoint being called
			{
				ActionResult result = ValidationProblem(ModelState);
				if (result is ObjectResult objectResult)
				{
					objectResult.StatusCode = (int)HttpStatusCode.BadRequest;
				}
				return result;
			}

			updateModel.CopyPropertiesTo(dbModel);
			baseAppDbContextActions.Update(dbModel);
			if (await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
			{
				return Ok(dbModel);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return NoContent();
	}
}
