namespace MiniUrl.Models;

public class UrlViewsUpdateRequest
{
    public Guid UrlMappingId { get; set; }

    public long UpdatedViewsCount { get; set; }

    public DateTime LastViewedDate { get; set; }
}