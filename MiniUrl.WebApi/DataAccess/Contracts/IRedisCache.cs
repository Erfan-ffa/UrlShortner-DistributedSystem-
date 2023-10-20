namespace MiniUrl.DataAccess.Contracts;

public interface IRedisCache
{
    Task<T> ReadObject<T>(string key, int timeout = 15000);

    Task<bool> KeyExists(string key, int timeout = 15000);
    
    Task<bool> WriteObject<T>(string key, T obj, double expirationSeconds = 420, int timeout = 15000, bool shouldExpire = true);
    
    Task KeyDelete(string key);

    bool BulkWrite<T>(Dictionary<string, T> pairs, double expirationSeconds = 60 * 20, int timeout = 15000);
    Task<long> IncrementValueByOneAsync(string key, int timeout = 15000);
}