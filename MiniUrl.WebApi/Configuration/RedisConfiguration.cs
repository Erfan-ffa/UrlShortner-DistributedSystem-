using MiniUrl.DataAccess.RedisDatabase;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

namespace MiniUrl.Configuration;

public static class RedisConfiguration
{
    public static void ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSetting = new RedisSetting();
        configuration.GetSection(nameof(RedisSetting)).Bind(redisSetting);

        var endpoints1 = new List<string>
        {
            $"{redisSetting.Masters[0].Ip}:{redisSetting.Masters[0].Port}",
            $"{redisSetting.Slaves[0].Ip}:{redisSetting.Slaves[0].Port}",
        };
        
        var endpoints2 = new List<string>
        {
            $"{redisSetting.Masters[1].Ip}:{redisSetting.Masters[1].Port}",
            $"{redisSetting.Slaves[1].Ip}:{redisSetting.Slaves[1].Port}",
        };
        
        var multiplexer = RedisMultiplexerFactory.GetMultiplexer(redisSetting, endpoints1);
        var multiplexer2 = RedisMultiplexerFactory.GetMultiplexer(redisSetting, endpoints2);
        var multiplexers = new List<RedLockMultiplexer>
        {
            multiplexer, multiplexer2
        };
        var redLockFactory = RedLockFactory.Create(multiplexers);

        services.AddSingleton(redisSetting);
        services.AddSingleton<IDistributedLockFactory>(redLockFactory);
    }
}