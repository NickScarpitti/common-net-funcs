using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CommonNetFuncs.Web.Api.OpenApiTransformers;

public sealed class HeaderTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new Microsoft.OpenApi.Any.OpenApiString("application/json")
            }
        });
        return Task.CompletedTask;
    }
}
