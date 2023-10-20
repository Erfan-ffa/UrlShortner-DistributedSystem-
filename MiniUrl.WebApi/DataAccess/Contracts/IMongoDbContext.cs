using MongoDB.Driver;

namespace MiniUrl.DataAccess.Contracts;

public interface IMongoDbContext
{
    IMongoClient Client { get;}
    
    public IMongoDatabase Database { get; }
    
    IClientSessionHandle CurrentClientSessionHandle  { get; }
    
    Task<bool> WithTransactionAsync(Func<Task> func, ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default);
}