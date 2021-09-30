using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    public static class AsyncObjectUpdate
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static async Task ObjectUpdate<T, UT>(T obj, string propertyName, Task<UT> task)
        {
            try
            {
                IEnumerable<PropertyInfo> props = obj.GetType().GetProperties();
                PropertyInfo prop = props.Where(x => x.Name.StrEq(propertyName)).FirstOrDefault();

                var value = await task;
                prop.SetValue(obj, value);
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
        }

        public static async Task ObjectFill<T, UT>(T obj, Task<UT> task)
        {
            try
            {
                var resultObject = await task;
                if (resultObject != null)
                {
                    resultObject.CopyPropertiesTo(obj);
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
        }

        public static async Task ObjectFill<T>(List<T> obj, Task<List<T>> task)
        {
            try
            {
                var resultObject = await task;
                if (resultObject != null)
                {
                    obj.AddRange(resultObject);
                }
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
        }

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
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
        }
    }
}
