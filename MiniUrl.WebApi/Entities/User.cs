namespace MiniUrl.Entities;

public class User : Entity
{
    public string UserName { get; set; }

    public string PasswordHash { get; set; }

    public string PhoneNumber { get; set; }
}