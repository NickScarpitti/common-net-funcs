<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;

namespace CommonNetFuncs.Web.Requests;

public static class JsonPatchFormatter
{
    /// <summary>
    /// <para>Use as InputFormatter in startup.cs ConfigureServices method</para>
    /// <para>Eg. options.InputFormatters.Insert(0, JsonPatchFormatter.JsonPatchInputFormatter());</para>
    /// </summary>
    public static NewtonsoftJsonPatchInputFormatter JsonPatchInputFormatter()
    {
        ServiceProvider builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson(options =>
            {
                //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            })
            .Services.BuildServiceProvider();

        return builder
            .GetRequiredService<IOptions<MvcOptions>>()
            .Value
            .InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }
}
=======
﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;

namespace CommonNetFuncs.Web.Requests;

public static class JsonPatchFormatter
{
    /// <summary>
    /// <para>Use as InputFormatter in startup.cs ConfigureServices method</para>
    /// <para>Eg. options.InputFormatters.Insert(0, JsonPatchFormatter.JsonPatchInputFormatter());</para>
    /// </summary>
    public static NewtonsoftJsonPatchInputFormatter JsonPatchInputFormatter()
    {
        ServiceProvider builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson(options =>
            {
                //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            })
            .Services.BuildServiceProvider();

        return builder.GetRequiredService<IOptions<MvcOptions>>().Value.InputFormatters.OfType<NewtonsoftJsonPatchInputFormatter>()            .First();
    }
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
