using Hangfire;
using MiniUrl.Models;

namespace MiniUrl.Services.Jobs.Contracts;

[AutomaticRetry(Attempts = 3, DelaysInSeconds = new []{ 60 , 120 , 120 })]
public interface IUrlViewUpdater
{
    Task UpdateViewsAsync(UrlMappingData urlMappingData, string shortUrl, CancellationToken cancellationToken);
}