using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PQC.API.Filters;

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Console.WriteLine($"🔧 Aplicando security em: {context.ApiDescription.RelativePath}");

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            }
        };

        Console.WriteLine($"   Security aplicado: {operation.Security.Count} requirements");
    }
}