using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LankaConnect.API.Filters;

/// <summary>
/// Operation filter to handle IFormFile parameters in Swagger documentation
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the operation consumes multipart/form-data
        var formFileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile) || p.Type == typeof(IEnumerable<IFormFile>))
            .ToList();

        if (!formFileParameters.Any())
            return;

        // Clear existing parameters for form file parameters
        foreach (var param in formFileParameters)
        {
            var toRemove = operation.Parameters
                .Where(p => p.Name == param.Name)
                .ToList();

            foreach (var p in toRemove)
            {
                operation.Parameters.Remove(p);
            }
        }

        // Configure request body for multipart/form-data
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = formFileParameters.ToDictionary(
                            p => p.Name,
                            p => new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        ),
                        Required = formFileParameters
                            .Where(p => p.IsRequired)
                            .Select(p => p.Name)
                            .ToHashSet()
                    }
                }
            }
        };
    }
}
