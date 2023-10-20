using Hangfire;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Services.Jobs.Contracts;

namespace MiniUrl.Services.Jobs;

[AutomaticRetry(Attempts = 5, DelaysInSeconds = new []{ 60, 120, 180, 240, 300 })]
public class UnusedUrlsRemover : IUnusedUrlsRemover
{
    private readonly IUrlMappingRepository _urlMappingRepository;

    public UnusedUrlsRemover(IUrlMappingRepository urlMappingRepository)
    {
        _urlMappingRepository = urlMappingRepository;
    }

    public async Task RemoveUnusedUrls()
    {
        var hasRemoved = await _urlMappingRepository.RemoveOldUnusedUrlsAsync(CancellationToken.None);
        if (hasRemoved == false)
            throw new Exception();
    }
}