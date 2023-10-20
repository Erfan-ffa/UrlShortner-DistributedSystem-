using Hangfire;
using Hangfire.Common;
using MiniUrl.Services.Jobs.Contracts;

namespace MiniUrl.Services.Jobs;

public static class HangfireExtensions
{
    private const string DailyAt12PmCron = "0 0 * * *";
    
    public static void AddHangfireJobs(this IServiceCollection service, IServiceProvider serviceProvider)
    {
        var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();

        service.AddScoped<IUnusedUrlsRemover, UnusedUrlsRemover>();
        service.AddScoped<IUrlViewUpdater, UrlViewUpdater>();
        
        recurringJobManager.AddOrUpdate(
            nameof(UnusedUrlsRemover),
            Job.FromExpression<IUnusedUrlsRemover>(x => x.RemoveUnusedUrls()),
            DailyAt12PmCron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran") });
        
    }
}