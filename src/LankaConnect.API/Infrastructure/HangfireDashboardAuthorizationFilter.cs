using Hangfire.Dashboard;

namespace LankaConnect.API.Infrastructure;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// In production, this should be secured with proper authentication
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In Development: Allow all access
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        // In Staging/Production: Require authentication and Admin role
        // The user must be authenticated AND have the Admin role
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
