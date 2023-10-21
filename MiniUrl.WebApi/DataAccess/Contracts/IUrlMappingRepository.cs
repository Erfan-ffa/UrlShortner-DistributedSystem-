using MiniUrl.Entities;
using MiniUrl.Models;

namespace MiniUrl.DataAccess.Contracts;

public interface IUrlMappingRepository
{
    Task<IEnumerable<UrlMappingResponse>> GetUrlMappingsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> CreateUrlMappingsAsync(List<UrlMapping> urlMappings, CancellationToken cancellationToken);
    
    Task<long?> GetUrlViewsAsync(string shortUrl, CancellationToken cancellationToken);

    Task<string> GetRedirectUrlByShortUrlAsync(string shortUrl, CancellationToken cancellationToken);
    
    Task UpdateUrlViewsByMappingIdAsync(List<UrlViewsUpdateRequest> updateRequest, CancellationToken cancellationToken);

    Task<bool> RemoveOldUnusedUrlsAsync(CancellationToken cancellationToken);
}