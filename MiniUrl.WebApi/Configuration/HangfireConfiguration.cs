using Hangfire;
using Hangfire.Redis.StackExchange;
using MiniUrl.Configuration.Settings;
using MiniUrl.Utils.Middlewares;
using StackExchange.Redis;

namespace MiniUrl.Configuration;

public static class HangfireConfiguration
{
    public static void AddHangfireConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var hangFireSettings = configuration.GetRequiredSection(nameof(HangfireSetting)).Get<HangfireSetting>();
        services.AddHangfire((_, hangfireConfig) =>
        {
            var redisReadConfig = new ConfigurationOptions
            {
                User = hangFireSettings!.Username,
                Password = hangFireSettings.Password,
                DefaultDatabase = hangFireSettings.RedisDbNumber,
                AllowAdmin = true,
                AsyncTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds,
                SyncTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds,
                ConnectTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds,
                EndPoints =
                {
                    hangFireSettings.RedisConnectionString
                }
            };
            var hangfireMultiplexer = ConnectionMultiplexer.Connect(redisReadConfig);
            hangfireConfig.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(hangfireMultiplexer, new RedisStorageOptions
                {
                    Db = hangFireSettings.RedisDbNumber,
                })
                .UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = hangFireSettings.DefaultRetryCount
                });
        });

        services.AddHangfireServer();
    }

    public static void AddHangfireDashboard(this  IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/dashboard", new DashboardOptions()
        {
            Authorization = new[] {new HangfireAuthorizationFilter()},
            IgnoreAntiforgeryToken = true,
            StatsPollingInterval = 5000,
            DashboardTitle = "MiniUrl",
            DisplayStorageConnectionString = false
        });
    } 
}