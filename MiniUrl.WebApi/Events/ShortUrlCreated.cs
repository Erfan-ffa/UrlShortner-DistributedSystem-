namespace MiniUrl.Events;

public class ShortUrlCreated : BaseEvent
{
    public string ShortUrl { get; set; }   
    public string LongUrl { get; set; }

    public Guid OwnerId { get; set; }
    
    public DateTime CreationDateTime { get; set; }
}

public class ShortUrlCreated2 : BaseEvent
{
    public string ShortUrl { get; set; }   
    public string LongUrl { get; set; }

    public Guid OwnerId { get; set; }
    
    public DateTime CreationDateTime { get; set; }
}