using System.IdentityModel.Tokens.Jwt;

namespace MiniUrl.Services.Identity.Models;

public class AccessToken
{
    public string Token { get; set; }
    public string Type { get; set; }
    public int ExpirationTime { get; set; }

    public AccessToken(JwtSecurityToken securityToken)
    {
        Token = new JwtSecurityTokenHandler().WriteToken(securityToken);
        Type = "Bearer";
        ExpirationTime = (int)(securityToken.ValidTo - DateTime.UtcNow).TotalSeconds;
    }
}