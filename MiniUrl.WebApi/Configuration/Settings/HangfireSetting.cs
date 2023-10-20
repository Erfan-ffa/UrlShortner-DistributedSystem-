namespace MiniUrl.Configuration.Settings;

public class HangfireSetting
{
    public string RedisConnectionString { get; set; }
    
    public int RedisDbNumber { get; set; }
    
    public string Username { get; set; }
    
    public string Password { get; set; }
    
    public int DefaultRetryCount { get; set; }
}