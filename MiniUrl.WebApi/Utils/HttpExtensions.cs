using System.Collections.Specialized;
using System.Web;

namespace MiniUrl.Utils;

public static class HttpExtensions
{
    public static bool HasQueryParams(this string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var requestUri) == false)
            throw new Exception("Invalid url.");
        
        var requestQueryParams = HttpUtility.ParseQueryString(requestUri.Query);

        return requestQueryParams.HasKeys();
    }

    public static string GetRequestUrl(this HttpContext httpContext)
    {
        return string.Concat(httpContext.Request.Host, httpContext.Request.Path, httpContext.Request.QueryString);
    }

    public static NameValueCollection GetUrlQueryParams(this string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var requestUri) == false)
            throw new Exception("Invalid url.");
        
        var requestQueryParams = HttpUtility.ParseQueryString(requestUri.Query);

        return requestQueryParams;
    }

    public static void AddQueryParamsToUrl(this HttpContext httpContext, ref string url)
    {
        var requestUrl = httpContext.GetRequestUrl();
        if (requestUrl.HasQueryParams() == false)
            return;
        
        if (url.HasQueryParams())
        {
            var requestQueryParams = requestUrl.GetUrlQueryParams();
            foreach (var key in requestQueryParams.AllKeys)
            {
                var value = requestQueryParams[key];
                url += $"&{key}={value}";
            }
        }
        else
        {
            url += httpContext.Request.QueryString.ToString();
        }
    }
}