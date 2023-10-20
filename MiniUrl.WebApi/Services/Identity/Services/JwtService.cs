using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiniUrl.Entities;
using MiniUrl.Services.Identity.Contracts;
using MiniUrl.Services.Identity.Models;

namespace MiniUrl.Services.Identity.Services;

public class JwtService : IJwtService
{
    private readonly JwtSetting _jwtSetting;

    public JwtService(IOptionsMonitor<JwtSetting> jwtSetting)
    {
        _jwtSetting = jwtSetting.CurrentValue;
    }

    public string GenerateToken(User user)
    {
        var secretKey = Encoding.UTF8.GetBytes(_jwtSetting.SecretKey);
        var signingCredentials =
            new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

        // var encryptionKey = Encoding.UTF8.GetBytes(_jwtSetting.EncryptKey);
        // var encryptingCredentials = new EncryptingCredentials(
        //     new SymmetricSecurityKey(encryptionKey),
        //     SecurityAlgorithms.HmacSha256);

        var claims = GetClaims(user);
        var descriptor = GenerateSecurityTokenDescriptor(signingCredentials);
        descriptor.Subject = new ClaimsIdentity(claims);

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(descriptor);

        var jwt = tokenHandler.WriteToken(token);
        return jwt;
    }

    private SecurityTokenDescriptor GenerateSecurityTokenDescriptor(
        SigningCredentials signingCredentials)
        => new SecurityTokenDescriptor
        {
            Issuer = _jwtSetting.Issuer,
            Audience = _jwtSetting.Audience,
            IssuedAt = DateTime.Now,
            NotBefore = DateTime.Now.AddMinutes(_jwtSetting.NotBeforeMinutes),
            Expires = DateTime.Now.AddMinutes(_jwtSetting.ExpirationMinutes),
            SigningCredentials = signingCredentials
        };

    private static List<Claim> GetClaims(User user)
        => new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
        };
}