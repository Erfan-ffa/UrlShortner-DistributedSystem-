using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Services.Messaging.Consumers;

namespace MiniUrl.Services.Messaging;

public static class MessagingConfiguration
{
    public static void AddRabbitMqConfig(this IServiceCollection service, IConfiguration configuration)
    {
        var serviceProvider = service.BuildServiceProvider();
        var setting = configuration.GetSection(nameof(RabbitMqSetting)).Get<RabbitMqSetting>();
        var redisCache = serviceProvider.GetRequiredService<IRedisCache>();
        var usMappingRepository = serviceProvider.GetRequiredService<IUrlMappingRepository>();

        service.AddMassTransit(x => { x.UsingRabbitMq();});
        var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            
            cfg.Host(setting!.Host);
            cfg.Durable = true;
            
            cfg.ReceiveEndpoint(setting.ShortUrlCreatedQueueName, e =>
            {
                e.PrefetchCount = setting.PrefetchCount;
                e.Batch<ShortUrlCreated>(b =>
                {
                    b.TimeLimit = TimeSpan.FromMilliseconds(500);
                    b.MessageLimit = setting.PrefetchCount;
                    b.Consumer(() => new ShortUrlCreatedConsumer(usMappingRepository, redisCache));
                });
            });
            
            cfg.ReceiveEndpoint(setting.UlrViewsQueueName, e =>
            {
                e.PrefetchCount = setting.PrefetchCount;
                e.Batch<UrlViewsIncreased>(b =>
                {
                    b.TimeLimit = TimeSpan.FromSeconds(1);
                    b.MessageLimit = setting.PrefetchCount;
                    e.Consumer(() => new UrlViewsIncreasedConsumer(usMappingRepository));
                });
            });
        });

        busControl.Start();
    }
}