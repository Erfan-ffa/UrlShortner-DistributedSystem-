
using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Services.Messaging.Consumers;

namespace MiniUrl.Services.Messaging;

public static class MessagingConfiguration
{
    public static void AddRabbitMqConfig(this IServiceCollection service, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        var setting = configuration.GetSection(nameof(RabbitMqSetting)).Get<RabbitMqSetting>();
        var redisCache = serviceProvider.GetRequiredService<IRedisCache>();
        var usMappingRepository = serviceProvider.GetRequiredService<IUrlMappingRepository>();

        service.AddMassTransit(x => { x.UsingRabbitMq();});
        var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(setting!.Host);
            cfg.ReceiveEndpoint(setting.ShortUrlCreatedQueueName, e =>
            {
                e.PrefetchCount = setting.PrefetchCount;
                e.Batch<ShortUrlCreated>(b =>
                {
                    b.MessageLimit = 5;
                    b.TimeLimit = TimeSpan.FromSeconds(10);
                    b.Consumer(() => new ShortUrlCreatedConsumer(usMappingRepository, redisCache));
                });
            });
            
            cfg.ReceiveEndpoint(setting.UlrViewsQueueName, e =>
            {
                // e.Consumer(() => new UrlViewsIncreasedConsumer(usMappingRepository));
                e.PrefetchCount = 10;
                e.Batch<UrlViewsIncreased>(b =>
                {
                    b.MessageLimit = 5;
                    b.TimeLimit = TimeSpan.FromSeconds(60);
                    e.Consumer(() => new UrlViewsIncreasedConsumer(usMappingRepository));
                });
            });
        });

        busControl.Start();
    }
}