using Microsoft.Extensions.Options;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Models;
using MiniUrl.Services.Helpers;
using MiniUrl.Utils;
using MiniUrl.Utils.Cache;
using RedLockNet;

namespace MiniUrl.Services.ShorterService;

public class UrlShorter : IUrlShorter
{
    private static long _currentId;
    private static long _endId;
    private readonly CounterRange _counterRange;
    private readonly IRedisCache _cache;
    private readonly IDistributedLockFactory _distributedLock;

    public UrlShorter(IOptionsMonitor<CounterRange> counterRange, IRedisCache cache, IDistributedLockFactory distributedLock)
    {
        _cache = cache;
        _distributedLock = distributedLock;
        _counterRange = counterRange.CurrentValue;
    }

    public async Task<string> GenerateUniqueText()
    {
        if (_currentId == _endId)
            await SetStartAndEndIdAsync();

        var shortenText = Base62Generator.Generate(_currentId++);
        
        return shortenText;
    }

    private async Task SetStartAndEndIdAsync()
    {
        var idRange = await GetIdRangeAsync();
        _currentId = idRange.startId;
        _endId = idRange.endId;
    }

    private async Task<(long startId, long endId)> GetIdRangeAsync()
    {
        (long startId, long endId) result = (0, 0);

        const string counterRangeKey = CacheKeys.CounterRange;
        var committed = false;

        var counterRange = await _cache.ReadObject<string>(counterRangeKey);
        
        const int maxRetries = 5;
        for (var i = 0; i < maxRetries; i++)
        {
            await using var redLock = await _distributedLock.CreateLockAsync(counterRangeKey,
                expiryTime: TimeSpan.FromSeconds(30),
                waitTime: TimeSpan.FromSeconds(10),
                retryTime: TimeSpan.FromSeconds(5));
            
            if (redLock.IsAcquired == false)
                throw new Exception("An error occured please try again.");

            var startId = Convert.ToInt64(counterRange) + 1;
            var endId = startId + _counterRange.Increment - 1;
            result = (startId, endId);

            var isWrittenInCache = await _cache.WriteObject(counterRangeKey, endId.ToString(), shouldExpire: false);
            committed = isWrittenInCache;
        }

        if (committed == false)
            throw new Exception("An error occured please try again.");
        
        return result;
    }
}