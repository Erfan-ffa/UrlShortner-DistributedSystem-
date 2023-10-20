using Hangfire;
using Microsoft.Extensions.Options;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.DataAccess.MongoDatabase;
using MiniUrl.Entities;
using MiniUrl.Models;
using MiniUrl.Services.Jobs.Contracts;
using MiniUrl.Utils.Cache;
using MongoDB.Driver;
using RedLockNet;

namespace MiniUrl.DataAccess.Repositories;

public class UrlMappingRepository : IUrlMappingRepository
{
    private readonly IMongoDbContext _mongoTransactionHandler;
    private readonly IDistributedLockFactory _distributedLock;
    private readonly IUrlViewUpdater _urlViewerUpdater;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IConfiguration _configuration;
    private readonly IRedisCache _redisCache;
    private IMongoCollection<UrlMapping> UrlMappingCollection { get; }
    private IMongoCollection<UrlView> UrlViewCollection { get; }

    public UrlMappingRepository(IOptionsMonitor<MongoSetting> mongoSetting,
        IRedisCache redisCache, IDistributedLockFactory distributedLock,
        IBackgroundJobClient jobClient, IUrlViewUpdater urlViewerUpdater, 
        IMongoDbContext mongoTransactionHandler, IConfiguration configuration)
    {
        _redisCache = redisCache;
        _distributedLock = distributedLock;
        _jobClient = jobClient;
        _urlViewerUpdater = urlViewerUpdater;
        _mongoTransactionHandler = mongoTransactionHandler;
        _configuration = configuration;
        var mongoClient = new MongoClient(mongoSetting.CurrentValue.ConnectionString);
        var dbContext = mongoClient.GetDatabase(mongoSetting.CurrentValue.DatabaseName);
        UrlMappingCollection = dbContext.GetCollection<UrlMapping>(nameof(UrlMapping));
        UrlViewCollection = dbContext.GetCollection<UrlView>(nameof(UrlView));
    }

    public async Task<string> GetRedirectUrlByShortUrlAsync(string shortUrl,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UrlMapping(shortUrl);

        await using var redLock = await _distributedLock.CreateLockAsync(cacheKey,
            expiryTime: TimeSpan.FromSeconds(30),
            waitTime: TimeSpan.FromSeconds(10),
            retryTime: TimeSpan.FromSeconds(5));

        if (redLock.IsAcquired == false)
            throw new Exception("Failed to acquire the RedLock.");

        await IncreaseUrlViewsByOneAsync(shortUrl);

        var urlMappingData = await _redisCache.ReadObject<UrlMappingData>(cacheKey);

        if (urlMappingData is null)
            return (await CacheUrlMappingAsync(shortUrl, cacheKey, cancellationToken)).LongUrl;
        
        if (urlMappingData.HasSavedInDb == false)
            throw new Exception("Something wrong has happened for your url. Please try again.");
        
        if (urlMappingData.ShouldUpdateUrlViewsInDb == false)
            return urlMappingData.LongUrl;

        await DisableUpdateViewsInDbTemporarilyAsync(shortUrl, urlMappingData);
        EnqueueUrlViewUpdaterJob(urlMappingData, shortUrl, cancellationToken);
        return urlMappingData.LongUrl;
    }

    private async Task<long> IncreaseUrlViewsByOneAsync(string shortUrl)
    {
        var urlViewsCacheKey = CacheKeys.UrlViews(shortUrl);
        var viewsAfterIncrement = await _redisCache.IncrementValueByOneAsync(urlViewsCacheKey);
        return viewsAfterIncrement;
    }

    private void EnqueueUrlViewUpdaterJob(UrlMappingData urlMappingData, string shortUrl, CancellationToken cancellationToken)
    {
        var updateDatabaseInterval = _configuration.GetSection("TimeIntervalForSyncDatabaseWithCacheInSeconds").Value;
        _jobClient.Schedule(() =>
                _urlViewerUpdater.UpdateViewsAsync(urlMappingData, shortUrl, cancellationToken),
                delay: TimeSpan.FromSeconds(Convert.ToInt64(updateDatabaseInterval)));
    }

    private async Task DisableUpdateViewsInDbTemporarilyAsync(string shortUrl, UrlMappingData urlMappingData)
    {
        var cacheKey = CacheKeys.UrlMapping(shortUrl);
        var value = new UrlMappingData(urlMappingData.MappingId, urlMappingData.LongUrl, false);
        await _redisCache.WriteObject(cacheKey, value);
    }

    private async Task<UrlMappingData> CacheUrlMappingAsync(string shortUrl, string cacheKey, CancellationToken cancellationToken)
    {
        var urlMapping = await UrlMappingCollection
            .Find(x => x.ShortUrl.Equals(shortUrl))
            .FirstOrDefaultAsync(cancellationToken);

        if (urlMapping is null)
            throw new Exception("Invalid url.");

        var urlMappingData = new UrlMappingData(urlMapping.Id, urlMapping.LongUrl, false);
        await _redisCache.WriteObject(cacheKey, urlMappingData, CacheExpiration.OneDay);

        EnqueueUrlViewUpdaterJob(urlMappingData, shortUrl, cancellationToken);

        return urlMappingData;
    }

    public async Task<bool> CreateUrlMappingsAsync(List<UrlMapping> urlMappings, CancellationToken cancellationToken)
    {
        var hasCommitted = await _mongoTransactionHandler.WithTransactionAsync(
            () => CreateUrlMappings(urlMappings, cancellationToken)
            ,cancellationToken: cancellationToken);

        return hasCommitted;
    }

    private async Task CreateUrlMappings(List<UrlMapping> urlMappings, CancellationToken cancellationToken)
    {
        var urlMappingIds = new Guid[urlMappings.Count];

        for (var i = 0; i < urlMappings.Count; i++)
        {
            urlMappingIds[i] = Guid.NewGuid();
            urlMappings[i].Id = urlMappingIds[i];
        }

        await UrlMappingCollection.InsertManyAsync(urlMappings, cancellationToken: cancellationToken);

        var urlViews = urlMappings.Select(x => new UrlView
        {
            Id = Guid.NewGuid(),
            UrlMappingId = x.Id,
            Views = 0,
            CreationTime = DateTime.Now,
            LastViewedDate = DateTime.Now
        });

        await UrlViewCollection.InsertManyAsync(urlViews, cancellationToken: cancellationToken);
    }

    public async Task<List<UrlMapping>> GetUserUrls(Guid userId, CancellationToken cancellationToken)
    {
        var urlMapping = await UrlMappingCollection
            .Find(x => x.OwnerId.Equals(userId))
            .ToListAsync(cancellationToken);

        return urlMapping;
    }
    
    public async Task<long?> GetUrlViewsAsync(string shortUrl, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UrlViews(shortUrl);
        var urlViewsCount = await _redisCache.ReadObjectFromPersistInstance<long?>(cacheKey);
        if (urlViewsCount is not null)
            return urlViewsCount;

        var urlMapping = (await UrlMappingCollection
            .FindAsync(x => x.ShortUrl.Equals(shortUrl), cancellationToken: cancellationToken))
            .FirstOrDefault(cancellationToken);
        
        if (urlMapping is null)
            return default;

        var urlViews = (await UrlViewCollection
            .FindAsync(x => x.UrlMappingId.Equals(urlMapping.Id), cancellationToken: cancellationToken))
            .FirstOrDefault(cancellationToken);

        return urlViews.Views;
    }

    public async Task UpdateUrlViewsByMappingIdAsync(List<UrlViewsUpdateRequest> updateRequest, CancellationToken cancellationToken)
    {
        var urlMappingIds = updateRequest.Select(x => x.UrlMappingId);
        var filter = Builders<UrlView>.Filter.Where(x => urlMappingIds.Contains(x.UrlMappingId));

        var updateDefinitions =  updateRequest.Select(x =>
        {
            return Builders<UrlView>.Update
                .Set(urlView => urlView.Views, x.UpdatedViewsCount)
                .Set(urlView => urlView.LastViewedDate, x.LastViewedDate);
        });
        
        var update = Builders<UrlView>.Update.Combine(updateDefinitions);
        await UrlViewCollection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<bool> RemoveOldUnusedUrlsAsync(CancellationToken cancellationToken)
    {
        var hasTransactionCommitted = await _mongoTransactionHandler
            .WithTransactionAsync(() => RemoveUnusedUrlsAsync(cancellationToken),
            cancellationToken: cancellationToken);

        return hasTransactionCommitted;
    }
    
    private async Task RemoveUnusedUrlsAsync(CancellationToken cancellationToken)
    {
        var removeUrlViewsFilter = Builders<UrlView>.Filter.Where(x => x.LastViewedDate.AddYears(1) < DateTime.Now);
        
        var unusedUrlViewMappingIds = await GetUrlViewMappingIdsAsync(removeUrlViewsFilter, cancellationToken);
        var urlMappingsDeleteResult = await RemoveUnusedUrlMappingsAsync(unusedUrlViewMappingIds, cancellationToken);
        if (urlMappingsDeleteResult.IsAcknowledged == false)
            throw new Exception("Could not delete url mappings.");
        
        var urlViewsDeleteResult = await UrlViewCollection.DeleteManyAsync(removeUrlViewsFilter, cancellationToken: cancellationToken);
        if (urlViewsDeleteResult.IsAcknowledged == false)
            throw new Exception("Could not delete url views.");

        if (urlMappingsDeleteResult.DeletedCount != urlViewsDeleteResult.DeletedCount)
            throw new Exception("Invalid operation occured.");
        
        // TODO: Remove its cache too
    }

    private async Task<IEnumerable<Guid>> GetUrlViewMappingIdsAsync(FilterDefinition<UrlView> filter, CancellationToken cancellationToken)
    {
        var urlViewsCollection = await UrlViewCollection.FindAsync(filter, default, cancellationToken);
        var result =  await urlViewsCollection.ToListAsync(cancellationToken);

        return result.Select(x => x.UrlMappingId);
    }

    private async Task<DeleteResult> RemoveUnusedUrlMappingsAsync(IEnumerable<Guid> mappingIds, CancellationToken cancellationToken)
    {
        var removeUrlMappingsFilter = Builders<UrlMapping>.Filter.In(x => x.Id, mappingIds);
        var result = await UrlMappingCollection.DeleteManyAsync(removeUrlMappingsFilter, cancellationToken: cancellationToken);

        return result;
    }
}