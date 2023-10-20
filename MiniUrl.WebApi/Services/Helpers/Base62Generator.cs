using System.Text;

namespace MiniUrl.Services.Helpers;

public static class Base62Generator
{
    private static readonly char[] charSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public static string Generate(long id) {

        var size = id;

        var tinyUrl = new StringBuilder();

        while (size > 0) {
            tinyUrl.Append(charSet[size % 62]);
            size = size / 62;
        }

        return tinyUrl.ToString();
    }
}