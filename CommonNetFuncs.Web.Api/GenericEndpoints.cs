﻿using System.ComponentModel.DataAnnotations;
using System.Net;
using CommonNetFuncs.EFCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CommonNetFuncs.Core.Collections;
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
    /// <typeparam name="T">Type of entity being created</typeparam>
    /// <typeparam name="UT">DB Context to use for this operation</typeparam>
    /// <param name="models">Entities to create</param>
    /// <param name="baseAppDbContextActions">Instance of baseAppDbContextActions to use</param>
    /// <returns>Ok if successful, otherwise NoContent</returns>
    public async Task<ActionResult<List<T>>> CreateMany<T, UT>(IEnumerable<T> models, IBaseDbContextActions<T, UT> baseAppDbContextActions, bool removeNavigationProps = false) where T : class where UT : DbContext
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    public async Task<ActionResult<T>> Delete<T, UT>(T model, IBaseDbContextActions<T, UT> baseAppDbContextActions, bool removeNavigationProps = false) where T : class where UT : DbContext
    {
        try
        {
            baseAppDbContextActions.DeleteByObject(model, removeNavigationProps);
            if (await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
            {
                return Ok(model);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    public async Task<ActionResult<List<T>>> DeleteMany<T, UT>(IEnumerable<T> models, IBaseDbContextActions<T, UT> baseAppDbContextActions, bool removeNavigationProps = false) where T : class where UT : DbContext
    {
        try
        {
            if (models.Any() && baseAppDbContextActions.DeleteMany(models, removeNavigationProps) && await baseAppDbContextActions.SaveChanges().ConfigureAwait(false))
            {
                return Ok(models);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    public async Task<ActionResult<List<T>>> DeleteManyByKeys<T, UT>(IEnumerable<object> models, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    public async Task<ActionResult<T>> Patch<T, UT>(object primaryKey, JsonPatchDocument<T> patch, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        T? dbModel = await baseAppDbContextActions.GetByKey(primaryKey).ConfigureAwait(false);
        return await PatchInternal(dbModel, patch, baseAppDbContextActions).ConfigureAwait(false);
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
    public async Task<ActionResult<T>> Patch<T, UT>(object[] primaryKey, JsonPatchDocument<T> patch, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        T? dbModel = await baseAppDbContextActions.GetByKey(primaryKey).ConfigureAwait(false);
        return await PatchInternal(dbModel, patch, baseAppDbContextActions).ConfigureAwait(false);
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
    private async Task<ActionResult<T>> PatchInternal<T, UT>(T? dbModel, JsonPatchDocument<T> patch, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
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

            T updateModel = dbModel.DeepClone();

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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return NoContent();
    }
}
