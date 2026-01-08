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
        var environment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        // In Development OR Staging: Allow all access (temporary for Phase 6A.64 testing)
        // TODO: Revert Staging to Admin-only after Phase 6A.64 testing is complete
        if (environment.IsDevelopment() || environment.IsStaging())
        {
            return true;
        }

        // In Production: Require authentication and Admin role
        // The user must be authenticated AND have the Admin role
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
