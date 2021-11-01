using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    /// <summary>
    /// Helper functions that send requests to specified URI and return resulting values where applicable
    /// Source1: https://medium.com/@srikanth.gunnala/generic-wrapper-to-consume-asp-net-web-api-rest-service-641b50462c0
    /// Source2: https://stackoverflow.com/questions/43692053/how-can-i-create-a-jsonpatchdocument-from-comparing-two-c-sharp-objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class RestHelpers<T> where T : class
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //Use static client here instead of individual using statements to prevent maxing out the number of connections
        private static readonly HttpClient client = new();

        /// <summary>
        /// For getting the resources from a web api
        /// From Source1
        /// </summary>
        /// <param name="url">API Url</param>
        /// <returns>A Task with result object of type T</returns>
        /// <exception cref="HttpRequestException">Ignore.</exception>
        /// <exception cref="ObjectDisposedException">Ignore.</exception>
        public static async Task<T> Get(string url)
        {
            T result = null;
            try
            {
                HttpResponseMessage response = client.GetAsync(new Uri(url)).Result;
                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                    {
                        if (x.IsFaulted) throw x.Exception;
                        result = JsonConvert.DeserializeObject<T>(x.Result);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
            return result;
        }

        /// <summary>
        /// For creating a new item over a web api using POST
        /// From Source1
        /// </summary>
        /// <param name="apiUrl">API Url</param>
        /// <param name="postObject">The object to be created</param>
        /// <returns>A Task with created item</returns>
        /// <exception cref="HttpRequestException">Ignore.</exception>
        /// <exception cref="ObjectDisposedException">Ignore.</exception>
        public static async Task<T> PostRequest(string apiUrl, T postObject)
        {
            T result = null;
            try
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, postObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);
                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                    {
                        if (x.IsFaulted) throw x.Exception;
                        result = JsonConvert.DeserializeObject<T>(x.Result);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
            return result;
        }

        public static async Task<string> StringPostRequest(string apiUrl, T postObject)
        {
            string result = null;
            try
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, postObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);
                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                    {
                        if (x.IsFaulted) throw x.Exception;
                        result = x.Result;
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
            return result;
        }

        public static async Task<T> DeleteRequest(string apiUrl, T deleteObject)
        {
            T result = deleteObject;
            try
            {
                HttpResponseMessage response = await client.DeleteAsync(apiUrl).ConfigureAwait(false);
                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                    {
                        if (x.IsFaulted) throw x.Exception;
                        result = JsonConvert.DeserializeObject<T>(x.Result);
                    });
                }
                else
                {
                    result = null;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
                result = null;
            }
            return result;
        }

        /// <summary>
        /// For updating an existing item over a web api using PUT
        /// From Source1
        /// </summary>
        /// <param name="apiUrl">API Url</param>
        /// <param name="putObject">The object to be edited</param>
        /// <exception cref="HttpRequestException">Ignore.</exception>
        public static async Task PutRequest(string apiUrl, T putObject)
        {
            HttpResponseMessage response = await client.PutAsync(apiUrl, putObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// PatchRequest
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="patchDoc"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException">Ignore.</exception>
        public static async Task<T> PatchRequest(string apiUrl, HttpContent patchDoc)
        {
            T result = null;
            try
            {
                HttpResponseMessage response = await client.PatchAsync(apiUrl, patchDoc).ConfigureAwait(false);
                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                    {
                        if (x.IsFaulted) throw x.Exception;
                        result = JsonConvert.DeserializeObject<T>(x.Result);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
            return result;
        }

        /// <summary>
        /// Converts two like models to JObjects and passes them into the FillPatchForObject method to create a JSON patch document
        /// From Source2
        /// </summary>
        /// <param name="originalObject"></param>
        /// <param name="modifiedObject"></param>
        /// <returns></returns>
        public static JsonPatchDocument CreatePatch(object originalObject, object modifiedObject)
        {
            JObject original = JObject.FromObject(originalObject);
            JObject modified = JObject.FromObject(modifiedObject);

            JsonPatchDocument patch = new();
            FillPatchForObject(original, modified, patch, "/");

            return patch;
        }

        /// <summary>
        /// Compares two JObjects together and creates a JSON patch document for the differences
        /// From Source2
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="mod"></param>
        /// <param name="patch"></param>
        /// <param name="path"></param>
        private static void FillPatchForObject(JObject orig, JObject mod, JsonPatchDocument patch, string path)
        {
            var origNames = orig.Properties().Select(x => x.Name).ToArray();
            var modNames = mod.Properties().Select(x => x.Name).ToArray();

            // Names removed in modified
            foreach (var k in origNames.Except(modNames))
            {
                var prop = orig.Property(k);
                patch.Remove(path + prop.Name);
            }

            // Names added in modified
            foreach (var k in modNames.Except(origNames))
            {
                var prop = mod.Property(k);
                patch.Add(path + prop.Name, prop.Value);
            }

            // Present in both
            foreach (var k in origNames.Intersect(modNames))
            {
                var origProp = orig.Property(k);
                var modProp = mod.Property(k);

                if (origProp.Value.Type != modProp.Value.Type)
                {
                    patch.Replace(path + modProp.Name, modProp.Value);
                }
                else if ((!origProp.Value.ToString(Formatting.None).StrEq(modProp.Value.ToString(Formatting.None)) && origProp.Value.Type != JTokenType.Date))
                {
                    if (origProp.Value.Type == JTokenType.Object)
                    {
                        // Recurse into objects
                        FillPatchForObject(origProp.Value as JObject, modProp.Value as JObject, patch, path + modProp.Name + "/");
                    }
                    else
                    {
                        // Replace values directly
                        patch.Replace(path + modProp.Name, modProp.Value);
                    }
                }
                else if (origProp.Value.Type == JTokenType.Date && modProp.Value.Type == JTokenType.Date)
                {
                    string originalDts = origProp.Value.ToString(Formatting.None).Replace(@"""", "").Replace(@"\", "");
                    string modDts = modProp.Value.ToString(Formatting.None).Replace(@"""", "").Replace(@"\", "");

                    bool originalSucceed = DateTime.TryParse(originalDts, out DateTime originalDate);
                    bool modSucceed = DateTime.TryParse(modDts, out DateTime modDate);

                    if (modSucceed && originalDate != modDate)
                    {
                        // Replace values directly
                        patch.Replace(path + modProp.Name, modProp.Value);
                    }
                }
            }
        }
    }
}