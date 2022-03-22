using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    public static class AsyncObjectUpdate
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Task to update obj property asynchronously
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="UT"></typeparam>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task ObjectUpdate<T, UT>(T? obj, string propertyName, Task<UT> task)
        {
            try
            {
                IEnumerable<PropertyInfo>? props = obj?.GetType().GetProperties();
                if (props != null && props.Any())
                {
                    PropertyInfo? prop = props.Where(x => x.Name.StrEq(propertyName)).FirstOrDefault();
                    if (prop != null)
                    {
                        var value = await task;
                        prop.SetValue(obj, value);
                    }
                    else
                    {
                        throw new System.Exception("Invalid property name for object update");
                    }
                }
                else
                {
                    throw new System.Exception("Unable to get properties of object to update");
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "ObjectUpdate Error");
            }
        }

        /// <summary>
        /// Task to fill obj variable asynchronously
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="UT"></typeparam>
        /// <param name="obj"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task ObjectFill<T, UT>(T? obj, Task<UT> task)
        {
            try
            {
                if (obj != null)
                {
                    var resultObject = await task;
                    if (resultObject != null)
                    {
                        resultObject.CopyPropertiesTo(obj);
                    }
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "ObjectFill Error");
            }
        }

        /// <summary>
        /// Task to fill list obj variable asynchronously
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="UT"></typeparam>
        /// <param name="obj"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task ObjectFill<T>(List<T>? obj, Task<List<T>?> task)
        {
            try
            {
                if (obj != null)
                {
                    var resultObject = await task;
                    if (resultObject != null)
                    {
                        obj.AddRange(resultObject);
                    }
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "ObjectFill Error");
            }
        }

        public static async Task ObjectFill(DataTable dt, Task<DataTable> task)
        {
            try
            {
                DataTable resultTable = await task;
                if (resultTable != null)
                {
                    DataTableReader reader = resultTable.CreateDataReader();
                    dt.Load(reader);
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "ObjectFill Error");
            }
        }

        /// <summary>
        /// Task to fill a MemoryStream variable asynchronously
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="UT"></typeparam>
        /// <param name="obj"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Task ObjectFill(MemoryStream obj, Task<MemoryStream> task)
        {
            try
            {
                var resultObject = await task;
                if (resultObject != null)
                {
                    resultObject.WriteTo(obj);
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "ObjectFill Error");
            }
        }
    }
}
