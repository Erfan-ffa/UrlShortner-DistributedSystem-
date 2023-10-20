
using MassTransit;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Events;
using MiniUrl.Services.Messaging.Consumers;

namespace MiniUrl.Services.Messaging;

public static class MessagingConfiguration
{
    // public static void AddKafkaConfig(this IServiceCollection service, IConfiguration configuration)
    // {
    // public class LogBatchConsumer : IConsumer<Batch<ShortUrlCreated2>>
    // {
    //     public async Task Consume(ConsumeContext<Batch<ShortUrlCreated2>> context)
    //     {
    //         StringBuilder builder = new StringBuilder();
    //
    //         for(int i = 0; i < context.Message.Length; i++)
    //         {
    //             builder.Append(context.Message[i].Message.LongUrl);
    //         }
    //
    //         Console.WriteLine(builder.ToString());
    //     }
    // }
    //     // var setting = configuration.GetSection(nameof(KafkaSetting)).Get<KafkaSetting>();
    //
    //     service.AddMassTransit(x =>
    //     {
    //         x.UsingInMemory();
    //         x.AddRider(rider =>
    //         {
    //             // rider.AddConsumer<ShortUrlCreatedConsumer>();
    //             rider.AddConsumer<LogBatchConsumer>();
    //             // rider.AddProducer<ShortUrlCreated>(setting.Topic);
    //             rider.AddProducer<ShortUrlCreated2>(nameof(ShortUrlCreated2));
    //             
    //             rider.UsingKafka((context, k) =>
    //             {
    //                 k.Host("setting.Host");
    //                 
    //                 k.TopicEndpoint<ShortUrlCreated2>(nameof(ShortUrlCreated2), "MiniUrlConsumer2", e =>
    //                 {
    //                     e.CreateIfMissing();
    //                     e.ConfigureConsumer<LogBatchConsumer>(context);
    //                     e.AutoOffsetReset = AutoOffsetReset.Earliest;
    //                     // e.MessageLimit TODO: Set This MessageLimit
    //                     // e.QueuedMinMessages TODO: Set This MessageLimit
    //                     e.PrefetchCount = 2;
    //
    //                     // e.Batch<ShortUrlCreated2>(b =>
    //                     // {
    //                     //     b.MessageLimit = 10;
    //                     //
    //                     //     // end the batch early if at least one message has been received and the 
    //                     //     // time limit is reached.
    //                     //     b.TimeLimit = TimeSpan.FromMicroseconds(500);
    //                     //
    //                     //
    //                     //     // end the batch early if at least one message has been received and the 
    //                     //     // time limit is reached.
    //                     //
    //                     //     b.Consumer(() => new LogBatchConsumer());
    //                     // });
    //
    //                 });
    //             });
    //         });
    //     });
    // }
    
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