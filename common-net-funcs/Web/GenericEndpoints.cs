using System.ComponentModel.DataAnnotations;
using Common_Net_Funcs.EFCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Common_Net_Funcs.Tools.DeepCloneExpressionTreeHelpers;
using static Common_Net_Funcs.Tools.ObjectHelpers;
using static Common_Net_Funcs.Tools.DebugHelpers;

namespace Common_Net_Funcs.Web;
public class GenericEndpoints : ControllerBase
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public async Task<ActionResult<List<T>>> CreateMany<T, UT>(IEnumerable<T> models, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        try
        {
            await baseAppDbContextActions.CreateMany(models);
            if (await baseAppDbContextActions.SaveChanges())
            {
                return Ok(models);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return NoContent();
    }

    public async Task<ActionResult<T>> Delete<T, UT>(T model, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        try
        {
            baseAppDbContextActions.DeleteByObject(model);
            if (await baseAppDbContextActions.SaveChanges())
            {
                return Ok(model);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return NoContent();
    }

    public async Task<ActionResult<List<T>>> DeleteMany<T, UT>(IEnumerable<T> models, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        try
        {
            if (models.Any())
            {
                baseAppDbContextActions.DeleteMany(models);
                if (await baseAppDbContextActions.SaveChanges())
                {
                    return Ok(models);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return NoContent();
    }

    public async Task<ActionResult<T>> Patch<T, UT>(object primaryKey, JsonPatchDocument<T> patch, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        T? dbModel = await baseAppDbContextActions.GetByKey(primaryKey);
        return await PatchInternal(dbModel, patch, baseAppDbContextActions);
    }

    public async Task<ActionResult<T>> Patch<T, UT>(object[] primaryKey, JsonPatchDocument<T> patch, IBaseDbContextActions<T, UT> baseAppDbContextActions) where T : class where UT : DbContext
    {
        T? dbModel = await baseAppDbContextActions.GetByKey(primaryKey);
        return await PatchInternal(dbModel, patch, baseAppDbContextActions);
    }

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

            T? updateModel = dbModel.DeepClone();

            if (updateModel == null)
            {
                return NoContent();
            }

            patch.ApplyTo(updateModel);

            List<ValidationResult> failedValidations = new();
            Validator.TryValidateObject(updateModel, new(updateModel), failedValidations);
            if (failedValidations.AnyFast())
            //if (!TryValidateModel(updateModel)) //Only works when this method is the controller endpoint being called
            {
                return ValidationProblem(ModelState);
            }

            updateModel.CopyPropertiesTo(dbModel);
            baseAppDbContextActions.Update(dbModel);
            if (await baseAppDbContextActions.SaveChanges())
            {
                return Ok(dbModel);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return NoContent();
    }
}
