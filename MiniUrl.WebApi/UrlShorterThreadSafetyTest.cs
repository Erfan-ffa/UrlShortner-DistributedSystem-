using MiniUrl.Models;
using System.Collections.Concurrent;
using MiniUrl.DataAccess.Contracts;
using MiniUrl.Services.ShorterService;

namespace MiniUrl;

public class UrlShorterThreadSafetyTest
{
    private readonly IUrlMappingRepository _url;
    private readonly IUrlShorter _urlShorter;

    public UrlShorterThreadSafetyTest(IUrlShorter urlShorter, IUrlMappingRepository url)
    {
        _urlShorter = urlShorter;
        _url = url;
    }

    public async Task TestThreadSafety()
    {
        var counterRange = new CounterRange
        {
            Increment = 10 // Adjust this value as needed
        };
        
        var concurrentRequests = 100; // Number of concurrent requests to simulate
        var results = new ConcurrentBag<string>();

        var userHandlers = new []
        {
            "users/okyrylchuk",
            "users/okyrylchuk",
            "users/jaredpar",
            "users/jaredpar",
            "users/davidfowl"
        };
        
        // await Parallel.ForEachAsync(userHandlers, new ParallelOptions{MaxDegreeOfParallelism = 5}, async (i, a) =>
        // {
        //     string shortUrl = await _urlShorter.ShortUrl("https://www.example.com");
        //     results.Add(shortUrl);
        // });

        // Check the results for any unexpected behavior
        // For example, check if there are any duplicate short URLs
        if (results.Count != results.Distinct().Count())
        {
            Console.WriteLine("Thread safety issue: Duplicate short URLs detected.");
        }
        else
        {
            Console.WriteLine("Thread safety test passed.");
        }
    }

    public  async Task IncreaseTest(Guid guid)
    {
        var tasks = new[]
        {
            IncrementCounterAsync(guid),
            IncrementCounterAsync(guid)
        };
    
        await Task.WhenAll(tasks);
    
        // Fetch and display the updated counter value
        var updatedDocument = _url.GetUrlViewsByMappingIdAsync(guid, CancellationToken.None);
        Console.WriteLine($"Updated Counter: {updatedDocument}");
    }
    
    async Task IncrementCounterAsync(Guid guid)
    {
        _url.IncreaseUrlViews(guid, 3);
    }
}
