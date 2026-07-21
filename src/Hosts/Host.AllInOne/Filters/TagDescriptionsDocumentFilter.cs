using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LankaConnect.API.Filters;

/// <summary>
/// Document filter to add tag definitions to OpenAPI specification
/// Ensures all controller tags are properly defined with descriptions for Swagger UI
/// </summary>
public class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Define all API tags with descriptions
        // This ensures Swagger UI properly groups and displays all endpoints
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "Analytics",
                Description = "Event analytics and organizer dashboard endpoints. Track views, registrations, and conversion metrics."
            },
            new OpenApiTag
            {
                Name = "Auth",
                Description = "Authentication and authorization endpoints. Handle user registration, login, password management, and profile."
            },
            new OpenApiTag
            {
                Name = "Businesses",
                Description = "Business directory and services endpoints. Manage business listings, images, and service offerings."
            },
            new OpenApiTag
            {
                Name = "Events",
                Description = "Event management endpoints. Create, publish, RSVP, and manage community events."
            },
            new OpenApiTag
            {
                Name = "Health",
                Description = "API health check endpoints. Monitor system status, database connectivity, and cache availability."
            },
            new OpenApiTag
            {
                Name = "Users",
                Description = "User profile management endpoints. Update profiles, preferences, cultural interests, and languages."
            }
        };
    }
}
