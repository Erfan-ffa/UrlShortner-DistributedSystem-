namespace MiniUrl.Services.Messaging;

public class RabbitMqSetting
{
    public string Host { get; set; }

    public string ShortUrlCreatedQueueName { get; set; }

    public string UlrViewsQueueName { get; set; }

    public int PrefetchCount { get; set; }
}