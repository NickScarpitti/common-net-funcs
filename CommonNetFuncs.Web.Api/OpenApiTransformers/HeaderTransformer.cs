using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CommonNetFuncs.Web.Api.OpenApiTransformers;

public sealed class HeaderTransformer : IOpenApiOperationTransformer
{
	public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
	{
		operation.Parameters ??= new List<IOpenApiParameter>();
		operation.Parameters.Add(new OpenApiParameter
		{
			Name = "Accept",
			In = ParameterLocation.Header,
			Required = true,
			Schema = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Default = JsonValue.Create("application/json")
			}
		});
		return Task.CompletedTask;
	}
}
