using System.ComponentModel.DataAnnotations;
using System.Net;
using CommonNetFuncs.Core;
using CommonNetFuncs.EFCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static CommonNetFuncs.Core.Copy;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.DeepClone.ExpressionTrees;
using static CommonNetFuncs.FastMap.FastMapper;

namespace CommonNetFuncs.Web.Api;

public sealed class GenericDotEndpoints : ControllerBase
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Basic endpoint to create more than one entity at a time
	/// </summary>
	/// <typeparam name="T">Type of entity being created</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="models">Entities to create</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<List<TOutDto>>> CreateMany<TModel, TContext, TInDto, TOutDto>(IEnumerable<TInDto> models, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions, bool removeNavigationProps = false)
		where TModel : class?, new() where TInDto : class, new() where TOutDto : class?, new() where TContext : DbContext
	{
		try
		{
			await baseAppDbContextActions.CreateMany(models.Select(x => x.FastMap<TInDto, TModel>()), removeNavigationProps).ConfigureAwait(false);
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
	/// <typeparam name="T">Type of entity being deleted</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="model">Entity to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<TOutDto>> Delete<TModel, TContext, TInDto, TOutDto>(TInDto model, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions, bool removeNavigationProps = false)
		where TModel : class?, new() where TInDto : class, new() where TOutDto : class?, new() where TContext : DbContext
	{
		try
		{
			baseAppDbContextActions.DeleteByObject(model.FastMap<TInDto, TModel>(), removeNavigationProps);
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
	/// <typeparam name="T">Type of entity being deleted</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="models">Entities to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<List<TOutDto>>> DeleteMany<TModel, TContext, TInDto, TOutDto>(IEnumerable<TInDto> models, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions, bool removeNavigationProps = false)
		where TModel : class?, new() where TInDto : class, new() where TOutDto : class?, new() where TContext : DbContext
	{
		try
		{
			if (models.Any() && baseAppDbContextActions.DeleteMany(models.Where(x => x != null).Select(x => x.FastMap<TInDto, TModel>()), removeNavigationProps) && await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
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
	/// <typeparam name="T">Type of entity being deleted</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="models">Entities to delete.</param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<List<TOutDto>>> DeleteManyByKeys<TModel, TContext, TOutDto>(IEnumerable<object> models, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TOutDto : class?, new() where TContext : DbContext
	{
		try
		{
			if (models.Any() && await baseAppDbContextActions.DeleteManyByKeys(models).ConfigureAwait(false)) //Does not work with PostgreSQL
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
	/// Basic endpoint to update an entity with a single field primary key
	/// </summary>
	/// <typeparam name="T">Type of entity being updated</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="primaryKey">Primary key of the entity to update</param>
	/// <param name="patch">Patch document containing the updates to be made to the entity.</param>>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<TOutDto>> Patch<TModel, TContext, TOutDto>(object primaryKey, JsonPatchDocument<TModel> patch, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TOutDto : class?, new() where TContext : DbContext
	{
		TModel? dbModel = await baseAppDbContextActions.GetByKey(primaryKey).ConfigureAwait(false);
		return await PatchInternal<TModel, TContext, TOutDto>(dbModel, patch, baseAppDbContextActions).ConfigureAwait(false);
	}

	/// <summary>
	/// Basic endpoint to update an entity with a multi-field primary key
	/// </summary>
	/// <typeparam name="T">Type of entity being updated</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="primaryKey">Ordered values comprising the key of the entity to update</param>
	/// <param name="patch">Patch document containing the updates to be made to the entity.</param>>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	public async Task<ActionResult<TOutDto>> Patch<TModel, TContext, TOutDto>(object[] primaryKey, JsonPatchDocument<TModel> patch, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TOutDto : class?, new() where TContext : DbContext
	{
		TModel? dbModel = await baseAppDbContextActions.GetByKey(primaryKey).ConfigureAwait(false);
		return await PatchInternal<TModel, TContext, TOutDto>(dbModel, patch, baseAppDbContextActions).ConfigureAwait(false);
	}

	/// <summary>
	/// Helper method to update an entity
	/// </summary>
	/// <typeparam name="T">Type of entity being updated</typeparam>
	/// <typeparam name="UT">DB Context to use for this operation</typeparam>
	/// <param name="dbModel"></param>
	/// <param name="patch"></param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	private async Task<ActionResult<TOutDto>> PatchInternal<TModel, TContext, TOutDto>(TModel? dbModel, JsonPatchDocument<TModel> patch, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TOutDto : class?, new() where TContext : DbContext
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

			TModel updateModel = dbModel.DeepClone();

			patch.ApplyTo(updateModel);

			List<ValidationResult> failedValidations = [];
			Validator.TryValidateObject(updateModel, new(updateModel), failedValidations);
			if (failedValidations.AnyFast())
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

	public async Task<ActionResult<TOutDto>> Update<TModel, TContext, TInDto, TOutDto>(object primaryKey, TInDto? inDto, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TInDto : class, new() where TOutDto : class?, new() where TContext : DbContext
	{
		TModel? dbModel = await baseAppDbContextActions.GetByKey(primaryKey).ConfigureAwait(false);
		return await UpdateInternal<TModel, TContext, TInDto, TOutDto>(dbModel, inDto, baseAppDbContextActions).ConfigureAwait(false);
	}

	public async Task<ActionResult<TOutDto>> Update<TModel, TContext, TInDto, TOutDto>(object[] primaryKey, TInDto? inDto, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TInDto : class, new() where TOutDto : class?, new() where TContext : DbContext
	{
		TModel? dbModel = await baseAppDbContextActions.GetByKey(primaryKey).ConfigureAwait(false);
		return await UpdateInternal<TModel, TContext, TInDto, TOutDto>(dbModel, inDto, baseAppDbContextActions).ConfigureAwait(false);
	}

	/// <summary>
	/// Helper method to update an entity
	/// </summary>
	/// <typeparam name="TModel">Type of entity being updated</typeparam>
	/// <typeparam name="TContext">DB Context to use for this operation</typeparam>
	/// <param name="dbModel"></param>
	/// <param name="patch"></param>
	/// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
	/// <returns>Ok if successful, otherwise NoContent</returns>
	private async Task<ActionResult<TOutDto>> UpdateInternal<TModel, TContext, TInDto, TOutDto>(TModel? dbModel, TInDto? inDto, IBaseDbContextActions<TModel, TContext> baseAppDbContextActions)
		where TModel : class?, new() where TInDto : class, new() where TOutDto : class?, new() where TContext : DbContext
	{
		try
		{
			if (dbModel == null)
			{
				return NoContent();
			}

			TModel updateModel = dbModel.DeepClone();
			inDto.CopyPropertiesTo(updateModel);

			List<ValidationResult> failedValidations = [];
			Validator.TryValidateObject(updateModel, new(updateModel), failedValidations);
			if (failedValidations.AnyFast())
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
				return Ok(dbModel.FastMap<TModel, TOutDto>());
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return NoContent();
	}
}
