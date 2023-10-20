using Microsoft.Extensions.Options;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.DataAccess.MongoDatabase;
using MiniUrl.Entities;
using MiniUrl.Utils;
using MiniUrl.Utils.Cache;
using MongoDB.Driver;

namespace MiniUrl.DataAccess.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IConfiguration _configuration;
    private readonly IRedisCache _redisCache;
    private IMongoCollection<User> Collection { get; }
    public UserRepository(IOptionsMonitor<MongoSetting> mongoSetting, IRedisCache redisCache, IConfiguration configuration)
    {
        _redisCache = redisCache;
        _configuration = configuration;
        var mongoClient = new MongoClient(mongoSetting.CurrentValue.ConnectionString);
        var dbContext = mongoClient.GetDatabase(mongoSetting.CurrentValue.DatabaseName);
        Collection = dbContext.GetCollection<User>(nameof(User));
    }
    
    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken)
    {
        user.Id = Guid.NewGuid();
        await Collection.InsertOneAsync(user, new InsertOneOptions(), cancellationToken);
        
        return user;
    }

    public async Task<bool> DoesUserExistByUserNameAsync(string username, CancellationToken cancellationToken)
    {
        return await Collection
            .Find(x => x.UserName.Equals(username))
            .AnyAsync(cancellationToken);
    }

    public async Task<User> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await Collection
            .Find(x => x.UserName.Equals(username))
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<long> GetUserAllowedUrlCreationCountAsync(Guid userId)
    {
        var userCreatedUrlsCount = await IncrementUserUrlCreationCapacityByOneAsync(userId);
        var userAllowedUrlCreation = Convert.ToInt64(_configuration.GetSection("UserAllowedUrlCapacityLimitation").Value);

        return userAllowedUrlCreation - userCreatedUrlsCount;
    }
    
    private async Task<long> IncrementUserUrlCreationCapacityByOneAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserRemainingUrlCapacity(userId);
        var valueAfterIncrement = await _redisCache.IncrementValueByOneAsync(cacheKey);

        return valueAfterIncrement;
    }
}