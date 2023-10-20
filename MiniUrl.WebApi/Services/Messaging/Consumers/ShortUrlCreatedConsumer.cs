using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Entities;
using MiniUrl.Events;
using MiniUrl.Models;
using MiniUrl.Utils;
using MiniUrl.Utils.Cache;

namespace MiniUrl.Services.Messaging.Consumers;

public class ShortUrlCreatedConsumer : IConsumer<Batch<ShortUrlCreated>>
{
    private readonly IRedisCache _redisCache;
    private readonly IUrlMappingRepository _urlMappingRepository;

    public ShortUrlCreatedConsumer(IUrlMappingRepository urlMappingRepository, IRedisCache redisCache)
    {
        _urlMappingRepository = urlMappingRepository;
        _redisCache = redisCache;
    }

    public async Task Consume(ConsumeContext<Batch<ShortUrlCreated>> context)
    {
        var urlMappings = GenerateUrlMappings(context.Message);
        var hasCommittedInDb = await AddUrlMappingsInDbAsync(urlMappings, context.CancellationToken);

        var keyValuePairs = GenerateKeyValuePairs(urlMappings);
        // var redisValue = hasCommittedInDb ? @event.Message.LongUrl : "0";
        AddUrlMappingsInCache(keyValuePairs);
    }

    private Dictionary<string, UrlMappingShit> GenerateKeyValuePairs(List<UrlMapping> urlMappings)
    {
        var result = new Dictionary<string, UrlMappingShit>();
        urlMappings.ToList().ForEach(x =>
        {
            var key = CacheKeys.UrlMapping(x.ShortUrl);
            var value = new UrlMappingShit(x.Id, x.LongUrl);
            result.Add(key, value);
        });

        return result;
    }

    private List<UrlMapping> GenerateUrlMappings(Batch<ShortUrlCreated> @events)
     => @events.Select(x 
         => new UrlMapping
         {
             ShortUrl = x.Message.ShortUrl,
             LongUrl = x.Message.LongUrl,
             OwnerId = x.Message.OwnerId,
             CreationTime = x.Message.CreationDateTime
         }).ToList();

    private async Task<bool> AddUrlMappingsInDbAsync(List<UrlMapping> urlMappings, CancellationToken cancellationToken)
    {
        var hasCommitted = false;
        try
        {
            await _urlMappingRepository.CreateUrlMappingsAsync(urlMappings, cancellationToken);
            hasCommitted = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return hasCommitted;
    }
    
    private void AddUrlMappingsInCache(Dictionary<string, UrlMappingShit> keyValuePairs)
        => _redisCache.BulkWrite(keyValuePairs, CacheExpiration.OneDay);
}