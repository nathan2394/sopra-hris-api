using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace sopra_hris_api.src.Helpers
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(IFormFile) || p.Type == typeof(IEnumerable<IFormFile>));
            if (fileParams.Any())
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParams.ToDictionary(
                                    p => p.Name,
                                    p => new OpenApiSchema { Type = "string", Format = "binary" }
                                ),
                                Required = fileParams.Where(p => p.Type == typeof(IFormFile)).Select(p => p.Name).ToHashSet()
                            }
                        }
                    }
                };
            }
        }
    }
}