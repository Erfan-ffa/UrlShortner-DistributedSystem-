using Hangfire;
using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Models;
using MiniUrl.Services.Jobs.Contracts;
using MiniUrl.Utils;
using MiniUrl.Utils.Cache;

namespace MiniUrl.Services.Jobs;

[DisableConcurrentExecution(120)]
public class UrlViewUpdater : IUrlViewUpdater
{
    private readonly IRedisCache _redisCache;
    private readonly IServiceProvider _serviceProvider;
    
    public UrlViewUpdater(IRedisCache redisCache, IServiceProvider serviceProvider)
    {
        _redisCache = redisCache;
        _serviceProvider = serviceProvider;
    }

    public async Task UpdateViewsAsync(UrlMappingData urlMappingData, string shortUrl, long lastViewsCount, CancellationToken cancellationToken)
    {
        var eventPublisher = _serviceProvider.GetRequiredService<IPublishEndpoint>();
        
        var urlViewsCacheKey = CacheKeys.UrlViews(shortUrl);
        var urlViewsCount = await _redisCache.ReadObject<long>(urlViewsCacheKey);
        var increasedViews = urlViewsCount - lastViewsCount;
        
        if(increasedViews <= 0)
            return;
        
        await eventPublisher.Publish(new UrlViewsIncreased 
        {
            UrlMappingId = urlMappingData.MappingId,
            ViewsToIncrement = increasedViews,
            LastViewedDate = DateTime.Now
        }, cancellationToken);

        var urlMappingCacheKey = CacheKeys.UrlMapping(shortUrl);
        urlMappingData.ShouldUpdateUrlViewsInDb = true;
        await _redisCache.WriteObject(urlMappingCacheKey, urlMappingData, CacheExpiration.OneDay);
    }
}