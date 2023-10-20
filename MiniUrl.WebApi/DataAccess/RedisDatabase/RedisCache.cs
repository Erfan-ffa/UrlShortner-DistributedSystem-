using System.Text.Json;
using Microsoft.Extensions.Options;
using MiniUrl.Configuration;
using MiniUrl.DataAccess.Contracts;
using StackExchange.Redis;

namespace MiniUrl.DataAccess.RedisDatabase;

public class RedisCache : IRedisCache
{
    private static ConnectionMultiplexer _unPersistMultiplexer;
    private static ConnectionMultiplexer _persistMultiplexer;
    private readonly RedisSetting _redisSetting;

    public RedisCache(IOptionsMonitor<RedisSetting> cacheSettings)
    {
        _redisSetting = cacheSettings.CurrentValue;
        ArgumentNullException.ThrowIfNull(cacheSettings);
        _unPersistMultiplexer = InitializeMultiplexer(_redisSetting.Masters[0], _redisSetting.Slaves[0]);
        _persistMultiplexer = InitializeMultiplexer(_redisSetting.Masters[1], _redisSetting.Slaves[1]);
    }

    private ConnectionMultiplexer InitializeMultiplexer(EndpointData masterSettings, EndpointData slaveSettings)
    {
        var endpoints = new List<string>
        {
            $"{masterSettings.Ip}:{masterSettings.Port}",
            $"{slaveSettings.Ip}:{slaveSettings.Port}"
        };

        return RedisMultiplexerFactory.GetMultiplexer(_redisSetting, endpoints);
    }

    public async Task<T> ReadObject<T>(string key, int timeout = 15000)
    {
        try
        {
            var cachedObject = await _unPersistMultiplexer.GetDatabase().StringGetAsync(key);
            if (string.IsNullOrEmpty(cachedObject) == false)
                return JsonSerializer.Deserialize<T>(cachedObject);
        }
        catch
        {
            return default;
        }

        return default;
    }

    public async Task<T> ReadObjectFromPersistInstance<T>(string key, int timeout = 15000)
    {
        try
        {
            var cachedObject = await _persistMultiplexer.GetDatabase().StringGetAsync(key);
            if (string.IsNullOrEmpty(cachedObject) == false)
                return JsonSerializer.Deserialize<T>(cachedObject);
        }
        catch
        {
            return default;
        }

        return default;
    }

    public async Task<bool> KeyExists(string key, int timeout = 15000)
        => await _unPersistMultiplexer.GetDatabase().KeyExistsAsync(key);

    public async Task<bool> WriteObject<T>(string key, T obj, double expirationSeconds = 60 * 20,
        int timeout = 15000, bool shouldExpire = true)
    {
        try
        {
            var content = JsonSerializer.Serialize(obj);
            var masterDatabase = _unPersistMultiplexer.GetDatabase();

            var result = await masterDatabase.StringSetAsync(key, content,
                expiry: shouldExpire ? TimeSpan.FromSeconds(expirationSeconds) : null,
                When.Always, CommandFlags.PreferMaster);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return true;
    }

    public bool BulkWrite<T>(Dictionary<string, T> pairs, double expirationSeconds = 60 * 20, int timeout = 15000)
    {
        try
        {
            var masterDatabase = _unPersistMultiplexer.GetDatabase();
            var batch = masterDatabase.CreateBatch();

            var tasks = new List<Task<bool>>();

            foreach (var pair in pairs)
            {
                var stringSetTask = batch.StringSetAsync(pair.Key, JsonSerializer.Serialize(pair.Value),
                    when: When.Always,
                    expiry: TimeSpan.FromSeconds(expirationSeconds));
                tasks.Add(stringSetTask);
            }

            batch.Execute();
            Task.WhenAll(tasks);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return false;
    }

    public async Task<long> IncrementValueByOneAsync(string key, int timeout = 15000)
    {
        var database = _persistMultiplexer.GetDatabase();
        var valueAfterIncrement = await database.StringIncrementAsync(key);
        return valueAfterIncrement;
    }

    public async Task KeyDelete(string key)
    {
        var database = _unPersistMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(key);
    }
}