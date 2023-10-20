namespace MiniUrl.Entities;

public class UrlView : Entity
{
    public Guid UrlMappingId { get; set; }

    public long Views { get; set; }

    public DateTime LastViewedDate { get; set; }
}