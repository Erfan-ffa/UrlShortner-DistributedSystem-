﻿namespace MiniUrl.Events;

public class UrlViewsIncreased : BaseEvent
{
    public Guid UrlMappingId { get; set; }

    public long ViewsToIncrement { get; set; }
    
    public DateTime LastViewedDate { get; set; }
}