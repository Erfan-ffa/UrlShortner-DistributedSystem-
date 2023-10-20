using MiniUrl.Entities;
using MiniUrl.Models;

namespace MiniUrl.DataAccess.Contracts;

public interface IUrlMappingRepository
{
    Task<bool> CreateUrlMappingsAsync(List<UrlMapping> urlMappings, CancellationToken cancellationToken);
    
    Task<List<UrlMapping>> GetUserUrls(Guid userId, CancellationToken cancellationToken);

    Task IncreaseUrlViews(Guid urlMappingId, long incrementValue);

    Task<long> GetUrlViewsByMappingIdAsync(Guid urlMappingId, CancellationToken cancellationToken);

    Task<string> GetRedirectUrlByShortUrlAsync(string shortUrl, CancellationToken cancellationToken);
    
    Task UpdateUrlViewsByMappingIdAsync(List<UrlViewsUpdateRequest> updateRequest, CancellationToken cancellationToken);

    Task<bool> RemoveOldUnusedUrlsAsync(CancellationToken cancellationToken);
}