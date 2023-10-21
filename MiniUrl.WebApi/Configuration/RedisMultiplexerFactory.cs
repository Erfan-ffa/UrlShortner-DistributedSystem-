using MiniUrl.Configuration.Settings;
using MiniUrl.DataAccess.RedisDatabase;
using StackExchange.Redis;

namespace MiniUrl.Configuration;

public static class RedisMultiplexerFactory
{
    public static ConnectionMultiplexer GetMultiplexer(RedisSetting redisSetting, List<string> endPoints)
    {
        var redisConfig = new ConfigurationOptions
        {
            User = redisSetting.Username,
            Password = redisSetting.Password,
            DefaultDatabase = redisSetting.DbNumber,
            AllowAdmin = true,
            AbortOnConnectFail = false,
            AsyncTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds,
            SyncTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds,
            ConnectTimeout = (int) TimeSpan.FromSeconds(10).TotalMilliseconds,
            EndPoints =
            {
               endPoints[0],
               endPoints[1],
            },
        };

        var multiplexer = ConnectionMultiplexer.Connect(redisConfig);

        return multiplexer;
    }
}