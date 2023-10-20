using System.Security.Cryptography;
using System.Text;

namespace MiniUrl.Services.Helpers;

public static class HashGenerator
{
    public static string GetSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var byteValue = Encoding.UTF8.GetBytes(input);
        var byteHash = sha256.ComputeHash(byteValue);
        return Convert.ToBase64String(byteHash);
    }
}