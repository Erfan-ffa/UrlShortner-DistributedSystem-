namespace MiniUrl.Services.Identity.Models;

public class JwtSetting
{
    public string Issuer { get; set; }

    public string Audience { get; set; }

    public string SecretKey { get; set; }
    
    public string EncryptKey { get; set; }

    public int NotBeforeMinutes { get; set; }
    
    public int ExpirationMinutes { get; set; }
}