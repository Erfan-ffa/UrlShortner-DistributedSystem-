namespace MiniUrl.Entities;

public abstract class Entity
{
    public Guid Id { get; set; }

    public DateTime CreationTime { get; set; }
}