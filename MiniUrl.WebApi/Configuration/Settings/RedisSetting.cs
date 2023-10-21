namespace MiniUrl.Configuration.Settings;

public class RedisSetting
{
    public string Username { get; set; }
    
    public string Password { get; set; }
    
    public int DbNumber { get; set; }
    
    public List<EndpointData> Slaves { get; set; }

    public List<EndpointData> Masters { get; set; }
}

public class EndpointData
{
    public string Ip { get; set; }

    public int Port { get; set; }
}