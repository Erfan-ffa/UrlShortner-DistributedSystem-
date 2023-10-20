namespace MiniUrl.Models;

public class UrlMappingData
{
    public UrlMappingData(Guid mappingId, string longUrl, bool shouldUpdateUrlViewsInDb = true)
    {
        MappingId = mappingId;
        LongUrl = longUrl;
        ShouldUpdateUrlViewsInDb = shouldUpdateUrlViewsInDb;
    }
    
    public Guid MappingId { get; }
    
    public string LongUrl { get; }

    public bool ShouldUpdateUrlViewsInDb { get; set; }

    public bool HasSavedInDb { get; set; } = true;
}