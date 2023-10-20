namespace MiniUrl.Entities;

public class UrlMapping : Entity
{
    public string ShortUrl { get; set; }
    
    public string LongUrl { get; set; }

    public Guid OwnerId { get; set; }
}