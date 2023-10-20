namespace MiniUrl.Utils.Cache;

public struct CacheKeys
{
    public static Func<string, string > OtpKey => phoneNumber => $"otp:phone_{phoneNumber}";
    public static Func<string, string> UserInfoKey => phoneNumber => $"user:info:phone_{phoneNumber}";
    public static Func<string, string> UrlMapping => shortUrl => $"{shortUrl}";
    public static Func<string, string> UrlViews => shortUrl => $"{shortUrl}:views";
    public static Func<Guid, string> UserRemainingUrlCapacity => userId => $"{userId}:limitation";
    
    public const string CounterRange = "counter_range";
}