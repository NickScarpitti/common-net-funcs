using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CommonNetFuncs.Web.Api.OpenApiTransformers;

public sealed class HeaderTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
	{
		operation.Parameters ??= [];
		operation.Parameters.Add(new OpenApiParameter
		{
			Name = "Accept",
			In = ParameterLocation.Header,
			Required = true,
			Schema = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Default = "application/json"
			}
		});
		return Task.CompletedTask;
	}
}
