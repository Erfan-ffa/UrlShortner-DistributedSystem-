namespace MiniUrl.Models;

public class UrlViewsUpdateRequest
{
    public Guid UrlMappingId { get; set; }

    public long ViewsToIncrement { get; set; }

    public DateTime LastViewedDate { get; set; }
}