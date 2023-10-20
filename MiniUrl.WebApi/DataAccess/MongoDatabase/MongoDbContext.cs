using Microsoft.Extensions.Options;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Entities;
using MongoDB.Driver;

namespace MiniUrl.DataAccess.MongoDatabase;

public class MongoDbContext : IMongoDbContext
{
    public IMongoClient Client { get;  }
    public IMongoDatabase Database { get;}
    public IClientSessionHandle CurrentClientSessionHandle { get; set; }

    public MongoDbContext(IOptionsMonitor<MongoSetting> mongoSetting)
    {
        Client = new MongoClient(mongoSetting.CurrentValue.ConnectionString);
        Database = Client.GetDatabase(mongoSetting.CurrentValue.DatabaseName);
        CreatUrlMappingCollectionRequiredIndexes();
        CreatUrlViewsCollectionRequiredIndexes();
    }

    private void CreatUrlMappingCollectionRequiredIndexes()
    {
        var indexKeys = Builders<UrlMapping>.IndexKeys.Ascending(nameof(UrlMapping.ShortUrl));
        var indexOptions = new CreateIndexOptions { Unique = true };
        var collection = Database.GetCollection<UrlMapping>(nameof(UrlMapping));
        
        collection.Indexes.CreateOne(new CreateIndexModel<UrlMapping>(indexKeys, indexOptions));
    }
    
    private void CreatUrlViewsCollectionRequiredIndexes()
    {
        var indexKeys = Builders<UrlView>.IndexKeys.Ascending(nameof(UrlView.UrlMappingId));
        var indexOptions = new CreateIndexOptions { Unique = false };
        var collection = Database.GetCollection<UrlView>(nameof(UrlView));
        
        collection.Indexes.CreateOne(new CreateIndexModel<UrlView>(indexKeys, indexOptions));        
    }
    
    public async Task<bool> WithTransactionAsync(Func<Task> func, ClientSessionOptions sessionOptions = null,
        TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(sessionOptions, transactionOptions, cancellationToken).ConfigureAwait(false);
        try
        {
            await func().ConfigureAwait(false);
            await CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch(Exception exception)
        {
            await RollBackTransactionAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        return true;
    }
    
    private async Task BeginTransactionAsync(ClientSessionOptions sessionOptions = null,
        TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default) //StartSession
    {
        if (CurrentClientSessionHandle != null)
            throw new InvalidOperationException();

        CurrentClientSessionHandle = await Client.StartSessionAsync(sessionOptions, cancellationToken).ConfigureAwait(false);
        CurrentClientSessionHandle.StartTransaction(transactionOptions);
    }

    private async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentClientSessionHandle == null)
            throw new InvalidOperationException("There is no active session.");

        await CurrentClientSessionHandle.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        CurrentClientSessionHandle.Dispose();
        CurrentClientSessionHandle = null;
    }

    private async Task RollBackTransactionAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("an error rkljdslfk jasdlkf jasdlk ");
        if (CurrentClientSessionHandle == null)
            throw new InvalidOperationException("There is no active session.");

        await CurrentClientSessionHandle.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
        CurrentClientSessionHandle.Dispose();
        CurrentClientSessionHandle = null;
    }
}