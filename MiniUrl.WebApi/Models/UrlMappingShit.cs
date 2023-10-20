namespace MiniUrl.Models;

public class UrlMappingShit
{
    public UrlMappingShit(Guid mappingId, string longUrl, bool shouldUpdateDb = true)
    {
        MappingId = mappingId;
        LongUrl = longUrl;
        ShouldUpdateDb = shouldUpdateDb;
    }
    
    public Guid MappingId { get; }
    
    public string LongUrl { get; }

    public bool ShouldUpdateDb { get; set; }
}