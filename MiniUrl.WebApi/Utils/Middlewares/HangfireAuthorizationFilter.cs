using Hangfire.Dashboard;

namespace MiniUrl.Utils.Middlewares;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
        => true;
}