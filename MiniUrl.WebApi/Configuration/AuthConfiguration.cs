using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MiniUrl.Services.Identity.Models;

namespace MiniUrl.Configuration;

public static class AuthConfiguration
{
    public static void AddJwtAuthentication(this IServiceCollection service, IConfiguration configuration)
    {
        var jwtSetting = configuration.GetSection("JwtSetting").Get<JwtSetting>();
        
        service.AddAuthentication(cfg =>
        {
            cfg.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            cfg.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var secretKey = Encoding.UTF8.GetBytes(jwtSetting.SecretKey);
            var encryptionKey = Encoding.UTF8.GetBytes(jwtSetting.EncryptKey);

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,  
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidAudience = jwtSetting.Audience,
                ValidateIssuer = true,
                ValidIssuer = jwtSetting.Issuer,
                TokenDecryptionKey = new SymmetricSecurityKey(encryptionKey)
            };

            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = validationParameters;
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context => throw new UnauthorizedAccessException("Authentication failed."),

                OnTokenValidated = context =>
                {
                    var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                    if (claimsIdentity?.Claims?.Any() != true)
                        context.Fail("This token has no claims.");
                    return Task.CompletedTask;
                }
            };
        });
    }
}
    