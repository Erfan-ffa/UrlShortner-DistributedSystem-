using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Models;

namespace MiniUrl.Services.Messaging.Consumers;

public class UrlViewsIncreasedConsumer : IConsumer<Batch<UrlViewsIncreased>>
{
    private readonly IUrlMappingRepository _urlMappingRepository;

    public UrlViewsIncreasedConsumer(IUrlMappingRepository urlMappingRepository)
    {
        _urlMappingRepository = urlMappingRepository;
    }

    public async Task Consume(ConsumeContext<Batch<UrlViewsIncreased>> context)
    {
        var urlViewsUpdateRequest = context.Message.Select(
            x => new UrlViewsUpdateRequest
            {
                UrlMappingId = x.Message.UrlMappingId,
                UpdatedViewsCount = x.Message.UpdatedViewsCount,
                LastViewedDate = DateTime.Now
            }).ToList();
        
        await _urlMappingRepository.UpdateUrlViewsByMappingIdAsync(urlViewsUpdateRequest, context.CancellationToken);
    }
}
