using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Entities;
using MiniUrl.Events;
using MiniUrl.Models;
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
        if (hasCommittedInDb == false)
            MarkUrlMappingsAsUncommitted(keyValuePairs);
        
        AddUrlMappingsInCache(keyValuePairs);
    }

    private void MarkUrlMappingsAsUncommitted(Dictionary<string, UrlMappingData> urlMappings)
    {
        foreach (var urlMappingsValue in urlMappings.Values) 
            urlMappingsValue.HasSavedInDb = false;
    }
    
    private Dictionary<string, UrlMappingData> GenerateKeyValuePairs(List<UrlMapping> urlMappings)
    {
        var result = new Dictionary<string, UrlMappingData>();
        urlMappings.ToList().ForEach(x =>
        {
            var key = CacheKeys.UrlMapping(x.ShortUrl);
            var value = new UrlMappingData(x.Id, x.LongUrl);
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
            hasCommitted = await _urlMappingRepository.CreateUrlMappingsAsync(urlMappings, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return hasCommitted;
    }
    
    private void AddUrlMappingsInCache(Dictionary<string, UrlMappingData> keyValuePairs)
        => _redisCache.BulkWrite(keyValuePairs, CacheExpiration.OneDay);
}